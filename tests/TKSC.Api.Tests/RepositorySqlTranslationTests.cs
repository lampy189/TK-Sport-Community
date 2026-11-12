using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class RepositorySqlTranslationTests
{
    // Stop after SQL translation, before opening any real connection. This catches changes
    // to projections that compile in C# or InMemory but cannot translate on SQL Server.
    private sealed class TranslationCompleteException : Exception;
    private sealed class StopBeforeOpen : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => throw new TranslationCompleteException();
    }

    [Fact]
    public async Task MovedAssessmentProjectionsStillTranslateAfterDistinctAndOrdering()
    {
        await using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
            .UseSqlServer("Server=unused;Database=translation_only;Integrated Security=True;TrustServerCertificate=True")
            .AddInterceptors(new StopBeforeOpen()).Options);
        var repository = new SkillAssessmentRepository(db, TimeProvider.System);
        await Assert.ThrowsAsync<TranslationCompleteException>(() => repository.GetAttendeesTicketsListAsync(1, 2, default));
        await Assert.ThrowsAsync<TranslationCompleteException>(() => repository.PlayerContextTicketsListAsync(1, 2, default,
            new Session { SportId = 1, SessionDate = new DateOnly(2026, 11, 11) }, 1));
        await Assert.ThrowsAsync<TranslationCompleteException>(() => repository.SessionsSessionsListAsync(1, default, new DateOnly(2026, 11, 11)));
    }

    [Fact]
    public async Task MovedSupportInboxAndMessagesStillTranslate()
    {
        await using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
            .UseSqlServer("Server=unused;Database=translation_only;Integrated Security=True;TrustServerCertificate=True")
            .AddInterceptors(new StopBeforeOpen()).Options);
        var repository = new TicketSupportRepository(db, TimeProvider.System);
        await Assert.ThrowsAsync<TranslationCompleteException>(() => repository.InboxTicketsListAsync());
        await Assert.ThrowsAsync<TranslationCompleteException>(() => repository.ListTicketSupportMessagesListAsync(1));
    }

    [Fact]
    public void FluentMappingsPreserveFormerEntityAnnotations()
    {
        using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
            .UseSqlServer("Server=unused;Database=translation_only;Integrated Security=True").Options);
        Assert.Null(db.Model.FindEntityType(typeof(Ticket))!.FindProperty(nameof(Ticket.LateEntryEligible)));
        Assert.Null(db.Model.FindEntityType(typeof(Ticket))!.FindProperty(nameof(Ticket.LateEntryEndsAt)));
        foreach (var (type, table) in new[] { (typeof(TicketChange), "ticket_changes"),
            (typeof(TicketFunding), "ticket_funding"), (typeof(TicketSupportMessage), "ticket_support_messages") })
        {
            var entity = db.Model.FindEntityType(type)!;
            Assert.Equal(table, entity.GetTableName());
            var store = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(table, entity.GetSchema());
            Assert.Equal("id", entity.FindProperty("Id")!.GetColumnName(store));
            Assert.Equal("ticket_id", entity.FindProperty("TicketId")!.GetColumnName(store));
        }
    }
}
