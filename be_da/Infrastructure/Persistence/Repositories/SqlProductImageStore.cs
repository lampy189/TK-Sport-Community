using System.Data;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;

namespace be_da.Infrastructure.Persistence.Repositories;

public sealed class SqlProductImageStore(IConfiguration configuration, IDataAccessContext scope) : IProductImageStore
{
    public async Task<ProductImage?> ReadAsync(long productId, long? buyerId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Sep490"));
        await SportCenterConnection.OpenAsync(connection, ct, scope);
        const string sql = """
            SELECT i.image_data, i.content_type
            FROM dbo.product_images i
            JOIN dbo.products p ON p.id = i.product_id
            WHERE p.id = @id AND (
                ((@accessVenue IS NULL OR p.venue_id = @accessVenue)
                 AND (@buyerId IS NULL OR p.is_active = 1))
                OR EXISTS (
                    SELECT 1 FROM dbo.order_items li
                    JOIN dbo.orders o ON o.id = li.order_id
                    WHERE li.product_id = p.id AND o.buyer_id = @buyerId
                )
            );
            """;
        await using var command = new SqlCommand(sql, connection);
        CenterAccess.Bind(command, scope, browsing: true);
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@buyerId", SqlDbType.BigInt).Value = (object?)buyerId ?? DBNull.Value;
        try
        {
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
            return await reader.ReadAsync(ct) ? new ProductImage(reader.GetFieldValue<byte[]>(0), reader.GetString(1)) : null;
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            // Older databases continue to use the bundled catalogue photos.
            return null;
        }
    }

    public async Task<ProductImageResult> SetAsync(long productId, ProductImage? image, CancellationToken ct)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Sep490"));
        await SportCenterConnection.OpenAsync(connection, ct, scope);
        const string sql = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            IF NOT EXISTS (
                SELECT 1 FROM dbo.products WITH (UPDLOCK, HOLDLOCK)
                WHERE id = @id AND venue_id = @accessVenue
            )
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT CAST(0 AS bit);
                RETURN;
            END;
            IF @data IS NULL
                DELETE dbo.product_images WHERE product_id = @id;
            ELSE
            BEGIN
                UPDATE dbo.product_images SET image_data = @data, content_type = @type WHERE product_id = @id;
                IF @@ROWCOUNT = 0
                    INSERT dbo.product_images(product_id, image_data, content_type) VALUES (@id, @data, @type);
            END;
            COMMIT TRANSACTION;
            SELECT CAST(1 AS bit);
            """;
        await using var command = new SqlCommand(sql, connection);
        CenterAccess.Bind(command, scope);
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@data", SqlDbType.VarBinary, -1).Value = (object?)image?.Data ?? DBNull.Value;
        command.Parameters.Add("@type", SqlDbType.VarChar, 16).Value = (object?)image?.ContentType ?? DBNull.Value;
        try
        {
            return await command.ExecuteScalarAsync(ct) is true ? ProductImageResult.Saved : ProductImageResult.Missing;
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return ProductImageResult.Unavailable;
        }
    }
}
