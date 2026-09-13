using Microsoft.EntityFrameworkCore;

namespace TextToSpeech.Infra.Jobs.Persistence;

internal static class OutboxStore
{
    // Hold this row lock while awaiting broker confirmation and committing PublishedAt in PostgreSQL.
    // Other publishers skip the locked row; process failure releases it for retry.
    internal static Task<OutboxMessage?> LockNextOutboxMessageAsync(
        this AppDbContext context, CancellationToken cancellationToken) =>
        context.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM jobs."OutboxMessage"
                WHERE "PublishedAt" IS NULL AND "AvailableAt" <= clock_timestamp()
                ORDER BY "AvailableAt", "CreatedAt", "Id"
                LIMIT 1 FOR UPDATE SKIP LOCKED
                """)
            .SingleOrDefaultAsync(cancellationToken);
}
