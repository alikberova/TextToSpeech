using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Jobs;

namespace TextToSpeech.Infra.Jobs.Persistence;

internal static class JobsDbContextExtensions
{
    // Call inside the transaction that checks and creates the job; the lock lasts until it ends.
    internal static Task LockSubmissionAsync(
        this AppDbContext context,
        JobSubmission submission,
        CancellationToken cancellationToken)
    {
        var key = JsonSerializer.Serialize(new[]
        {
            submission.OwnerId,
            submission.JobType,
            submission.IdempotencyKey
        });

        return context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
    }

    internal static void AddEvent(this AppDbContext context, BackgroundJob job, JobEventKind kind, DateTimeOffset now)
    {
        context.BackgroundJobEvents.Add(new BackgroundJobEvent
        {
            JobId = job.Id,
            AttemptId = job.CurrentAttemptId,
            Kind = kind,
            Status = job.Status,
            OccurredAt = now,
            ErrorCode = job.ErrorCode
        });
    }

    internal static void AddDispatch(this AppDbContext context, BackgroundJob job, DateTimeOffset now)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ContractVersion = JobDispatchMessage.CurrentVersion,
            CorrelationId = job.CorrelationId,
            CreatedAt = now,
            AvailableAt = job.AvailableAt
        });
    }

    internal static async Task<T> InTransactionAsync<T>(
        this AppDbContext context,
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var result = await action();

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    internal static Task<DateTimeOffset> GetDatabaseTimeAsync(
        this AppDbContext context,
        CancellationToken cancellationToken) =>
        // Read wall-clock time after locking, not the start time of a potentially long caller transaction.
        context.Database
            .SqlQuery<DateTimeOffset>($"SELECT clock_timestamp() AS \"Value\"")
            .SingleAsync(cancellationToken);
}
