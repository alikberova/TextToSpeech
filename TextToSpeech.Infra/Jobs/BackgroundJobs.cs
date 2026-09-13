using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra.Jobs;

public sealed class BackgroundJobs(AppDbContext context) : IBackgroundJobs
{
    private const string LeaseExpiredCode = "lease_expired";
    private const string UnsafeRetryCode = "retry_not_safe";

    public async Task<JobSnapshot?> GetAsync(Guid jobId, string ownerId, CancellationToken cancellationToken)
    {
        var job = await context.Set<BackgroundJob>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == jobId && x.OwnerId == ownerId, cancellationToken);

        return job?.Snapshot();
    }

    public Task<bool> RequestCancellationAsync(Guid jobId, string ownerId, CancellationToken cancellationToken) =>
        ChangeAsync(jobId, async (job, now) =>
        {
            if (job.OwnerId != ownerId || IsTerminal(job.Status))
            {
                return false;
            }

            if (job.CancellationRequestedAt.HasValue)
            {
                return true;
            }

            job.CancellationRequestedAt = now;

            context.AddEvent(job, JobEventKind.CancellationRequested, now);

            if (job.Status == JobStatus.Pending)
            {
                await FinishAsync(
                    job,
                    JobStatus.Cancelled,
                    AttemptStatus.Cancelled,
                    JobEventKind.Cancelled,
                    now,
                    cancellationToken);
            }

            return true;
        }, cancellationToken);

    public Task<JobExecution?> TryStartAsync(Guid jobId, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        ValidateLease(leaseDuration);

        return context.InTransactionAsync(async () =>
        {
            var job = await LockAsync(jobId, cancellationToken);
            var now = await context.GetDatabaseTimeAsync(cancellationToken);

            if (job is null ||
                job.Status != JobStatus.Pending ||
                job.AvailableAt > now ||
                job.CancellationRequestedAt.HasValue ||
                job.AttemptCount >= job.MaxAttempts)
            {
                return null;
            }

            var attempt = new BackgroundJobAttempt
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                Number = ++job.AttemptCount,
                Status = AttemptStatus.Running,
                StartedAt = now
            };

            context.Add(attempt);

            job.CurrentAttemptId = attempt.Id;
            job.LeaseExpiresAt = now + leaseDuration;
            job.Status = JobStatus.Running;
            job.Progress = 0;
            job.ErrorCode = null;

            context.AddEvent(job, JobEventKind.Started, now);

            await context.SaveChangesAsync(cancellationToken);

            return new JobExecution(job.Id, attempt.Id, job.OwnerId, job.Snapshot());
        }, cancellationToken);
    }

    public Task<bool> RenewAsync(
        Guid jobId,
        Guid attemptId,
        TimeSpan leaseDuration,
        int progress,
        CancellationToken cancellationToken)
    {
        ValidateLease(leaseDuration);
        ArgumentOutOfRangeException.ThrowIfNegative(progress);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(progress, 100);

        return ChangeAsync(jobId, (job, now) =>
        {
            if (!OwnsExecution(job, attemptId, now))
            {
                return Task.FromResult(false);
            }

            job.LeaseExpiresAt = now + leaseDuration;
            job.Progress = Math.Max(job.Progress, progress);

            return Task.FromResult(true);
        }, cancellationToken);
    }

    public Task<bool> CompleteAsync(Guid jobId, Guid attemptId, Guid resultId, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Completing a job requires the caller's result persistence transaction.");
        }

        return ChangeAsync(jobId, async (job, now) =>
        {
            if (!OwnsExecution(job, attemptId, now))
            {
                return false;
            }

            // The caller must persist the result in the same transaction before committing completion.
            job.ResultId = resultId;
            job.Progress = 100;

            await FinishAsync(
                job,
                JobStatus.Completed,
                AttemptStatus.Completed,
                JobEventKind.Completed,
                now,
                cancellationToken);

            return true;
        }, cancellationToken);
    }

    public Task<bool> FailAsync(
        Guid jobId,
        Guid attemptId,
        string errorCode,
        TimeSpan? safeRetryDelay,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        if (safeRetryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(safeRetryDelay));
        }

        return ChangeAsync(jobId, async (job, now) =>
        {
            if (!OwnsExecution(job, attemptId, now))
            {
                return false;
            }

            job.ErrorCode = errorCode;

            await FinishAsync(
                job,
                JobStatus.Failed,
                AttemptStatus.Failed,
                JobEventKind.Failed,
                now,
                cancellationToken);

            if (safeRetryDelay.HasValue &&
                !job.CancellationRequestedAt.HasValue &&
                job.AttemptCount < job.MaxAttempts)
            {
                ScheduleRetry(job, safeRetryDelay.Value, now);
            }

            return true;
        }, cancellationToken);
    }

    public Task<bool> AcknowledgeCancellationAsync(Guid jobId, Guid attemptId, CancellationToken cancellationToken) =>
        ChangeAsync(jobId, async (job, now) =>
        {
            if (!OwnsExecution(job, attemptId, now) || !job.CancellationRequestedAt.HasValue)
            {
                return false;
            }

            await FinishAsync(
                job,
                JobStatus.Cancelled,
                AttemptStatus.Cancelled,
                JobEventKind.Cancelled,
                now,
                cancellationToken);

            return true;
        }, cancellationToken);

    public Task<bool> RecoverExpiredAsync(Guid jobId, CancellationToken cancellationToken) =>
        ChangeAsync(jobId, async (job, now) =>
        {
            if (job.Status != JobStatus.Running || job.LeaseExpiresAt > now)
            {
                return false;
            }

            job.ErrorCode = LeaseExpiredCode;

            await FinishAsync(
                job,
                JobStatus.RecoveryRequired,
                AttemptStatus.Abandoned,
                JobEventKind.LeaseExpired,
                now,
                cancellationToken);

            return true;
        }, cancellationToken);

    public Task<bool> ResolveRecoveryAsync(Guid jobId, bool retryIsSafe, CancellationToken cancellationToken) =>
        ChangeAsync(jobId, (job, now) =>
        {
            if (job.Status != JobStatus.RecoveryRequired)
            {
                return Task.FromResult(false);
            }

            if (job.CancellationRequestedAt.HasValue)
            {
                job.Status = JobStatus.Cancelled;
            }
            else if (retryIsSafe && job.AttemptCount < job.MaxAttempts)
            {
                ScheduleRetry(job, TimeSpan.Zero, now);
            }
            else
            {
                job.Status = JobStatus.Failed;
                job.ErrorCode = UnsafeRetryCode;
            }

            job.FinishedAt = IsTerminal(job.Status) ? now : null;

            context.AddEvent(job, JobEventKind.RecoveryResolved, now);

            return Task.FromResult(true);
        }, cancellationToken);

    private async Task<bool> ChangeAsync(
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

    private async Task<BackgroundJob?> LockAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await context.Set<BackgroundJob>()
            .FromSqlInterpolated($"SELECT * FROM jobs.\"BackgroundJob\" WHERE \"Id\" = {jobId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (job is not null)
        {
            // A scoped context may already track an older snapshot; refresh after taking the lock.
            await context.Entry(job).ReloadAsync(cancellationToken);
        }

        return job;
    }

    private static bool OwnsExecution(BackgroundJob job, Guid attemptId, DateTimeOffset now) =>
        job.Status == JobStatus.Running &&
        job.CurrentAttemptId == attemptId &&
        job.LeaseExpiresAt > now;

    private async Task FinishAsync(
        BackgroundJob job,
        JobStatus status,
        AttemptStatus attemptStatus,
        JobEventKind eventKind,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (job.CurrentAttemptId.HasValue)
        {
            var attempt = await context.Set<BackgroundJobAttempt>()
                .SingleAsync(x => x.Id == job.CurrentAttemptId.Value, cancellationToken);

            attempt.Status = attemptStatus;
            attempt.FinishedAt = now;
            attempt.ErrorCode = job.ErrorCode;
        }

        job.Status = status;
        job.FinishedAt = IsTerminal(status) ? now : null;
        job.LeaseExpiresAt = null;

        context.AddEvent(job, eventKind, now);

        job.CurrentAttemptId = null;
    }

    private void ScheduleRetry(BackgroundJob job, TimeSpan delay, DateTimeOffset now)
    {
        job.Status = JobStatus.Pending;
        job.FinishedAt = null;
        job.AvailableAt = now + delay;

        context.AddEvent(job, JobEventKind.RetryScheduled, now);
        context.AddDispatch(job, now);
    }

    private static bool IsTerminal(JobStatus status) =>
        status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled;

    private static void ValidateLease(TimeSpan leaseDuration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseDuration, TimeSpan.Zero);
    }
}
