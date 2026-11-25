SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.product_images', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.product_images (
        product_id bigint NOT NULL CONSTRAINT PK_product_images PRIMARY KEY
            CONSTRAINT FK_product_images_product REFERENCES dbo.products(id) ON DELETE CASCADE,
        image_data varbinary(max) NOT NULL,
        content_type varchar(16) NOT NULL,
        CONSTRAINT CK_product_images_size CHECK (DATALENGTH(image_data) BETWEEN 1 AND 2097152),
        CONSTRAINT CK_product_images_type CHECK (content_type IN ('image/jpeg', 'image/png', 'image/webp'))
    );
END;

COMMIT TRANSACTION;
