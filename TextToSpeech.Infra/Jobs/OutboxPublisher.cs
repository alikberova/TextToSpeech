using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;
using TextToSpeech.Infra.Jobs.Transport;

namespace TextToSpeech.Infra.Jobs;

public sealed class OutboxPublisher(AppDbContext context, IJobDispatchPublisher publisher)
{
    public async Task<bool> PublishNextAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entry = await context.LockNextOutboxMessageAsync(cancellationToken);

        if (entry is null)
        {
            return false;
        }

        var message = new JobDispatchMessage(entry.JobId, entry.ContractVersion, entry.CorrelationId);

        await publisher.PublishAsync(entry.Id, message, cancellationToken);

        entry.PublishedAt = await context.GetDatabaseTimeAsync(cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}
