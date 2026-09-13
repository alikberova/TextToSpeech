using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra.Jobs;

public sealed class BackgroundJobStore(AppDbContext context)
{
    public bool HasCurrentTransaction => context.Database.CurrentTransaction is not null;

    internal async Task<JobSnapshot?> GetSnapshotAsync(
        Guid jobId,
        string ownerId,
        CancellationToken cancellationToken)
    {
        var job = await context.BackgroundJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == jobId && x.OwnerId == ownerId, cancellationToken);

        return job?.Snapshot();
    }

    internal Task<TResult> InTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken) =>
        context.InTransactionAsync(action, cancellationToken);

    internal async Task<bool> ChangeAsync(
        Guid jobId,
        Func<BackgroundJob, DateTimeOffset, Task<bool>> change,
        CancellationToken cancellationToken)
    {
        return await context.InTransactionAsync(async () =>
        {
            var job = await LockAsync(jobId, cancellationToken);

            if (job is null)
            {
                return false;
            }

            var now = await context.GetDatabaseTimeAsync(cancellationToken);

            if (!await change(job, now))
            {
                return false;
            }

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }, cancellationToken);
    }

    internal async Task<BackgroundJob?> LockAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await context.BackgroundJobs
            .FromSqlInterpolated($"SELECT * FROM jobs.\"BackgroundJob\" WHERE \"Id\" = {jobId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (job is not null)
        {
            // A scoped context may already track an older snapshot; refresh after taking the lock.
            await context.Entry(job).ReloadAsync(cancellationToken);
        }

        return job;
    }

    internal Task<DateTimeOffset> GetDatabaseTimeAsync(CancellationToken cancellationToken) =>
        context.GetDatabaseTimeAsync(cancellationToken);

    internal void AddAttempt(BackgroundJobAttempt attempt)
    {
        context.BackgroundJobAttempts.Add(attempt);
    }

    internal Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
