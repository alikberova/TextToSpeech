using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra.Jobs;

public sealed class BackgroundJobs(
    BackgroundJobStore jobStore,
    BackgroundJobStateTransitions transitions) : IBackgroundJobs
{
    private const string LeaseExpiredCode = "lease_expired";
    private const string UnsafeRetryCode = "retry_not_safe";

    public Task<JobSnapshot?> GetAsync(Guid jobId, string ownerId, CancellationToken cancellationToken) =>
        jobStore.GetSnapshotAsync(jobId, ownerId, cancellationToken);

    public Task<bool> RequestCancellationAsync(Guid jobId, string ownerId, CancellationToken cancellationToken) =>
        jobStore.ChangeAsync(jobId, async (job, now) =>
        {
            if (job.OwnerId != ownerId || transitions.IsTerminal(job.Status))
            {
                return false;
            }

            if (job.CancellationRequestedAt.HasValue)
            {
                return true;
            }

            job.CancellationRequestedAt = now;

            transitions.AddEvent(job, JobEventKind.CancellationRequested, now);

            if (job.Status == JobStatus.Pending)
            {
                await transitions.FinishAsync(
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

        return jobStore.InTransactionAsync(async () =>
        {
            var job = await jobStore.LockAsync(jobId, cancellationToken);
            var now = await jobStore.GetDatabaseTimeAsync(cancellationToken);

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

            jobStore.AddAttempt(attempt);

            job.CurrentAttemptId = attempt.Id;
            job.LeaseExpiresAt = now + leaseDuration;
            job.Status = JobStatus.Running;
            job.Progress = 0;
            job.ErrorCode = null;

            transitions.AddEvent(job, JobEventKind.Started, now);

            await jobStore.SaveChangesAsync(cancellationToken);

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

        return jobStore.ChangeAsync(jobId, (job, now) =>
        {
            if (!transitions.OwnsExecution(job, attemptId, now))
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
        if (!jobStore.HasCurrentTransaction)
        {
            throw new InvalidOperationException(
                "Completing a job requires the caller's result persistence transaction.");
        }

        return jobStore.ChangeAsync(jobId, async (job, now) =>
        {
            if (!transitions.OwnsExecution(job, attemptId, now))
            {
                return false;
            }

            // The caller must persist the result in the same transaction before committing completion.
            job.ResultId = resultId;
            job.Progress = 100;

            await transitions.FinishAsync(
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

        return jobStore.ChangeAsync(jobId, async (job, now) =>
        {
            if (!transitions.OwnsExecution(job, attemptId, now))
            {
                return false;
            }

            job.ErrorCode = errorCode;

            await transitions.FinishAsync(
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
                transitions.ScheduleRetry(job, safeRetryDelay.Value, now);
            }

            return true;
        }, cancellationToken);
    }

    public Task<bool> AcknowledgeCancellationAsync(Guid jobId, Guid attemptId, CancellationToken cancellationToken) =>
        jobStore.ChangeAsync(jobId, async (job, now) =>
        {
            if (!transitions.OwnsExecution(job, attemptId, now) || !job.CancellationRequestedAt.HasValue)
            {
                return false;
            }

            await transitions.FinishAsync(
                job,
                JobStatus.Cancelled,
                AttemptStatus.Cancelled,
                JobEventKind.Cancelled,
                now,
                cancellationToken);

            return true;
        }, cancellationToken);

    public Task<bool> RecoverExpiredAsync(Guid jobId, CancellationToken cancellationToken) =>
        jobStore.ChangeAsync(jobId, async (job, now) =>
        {
            if (job.Status != JobStatus.Running || job.LeaseExpiresAt > now)
            {
                return false;
            }

            job.ErrorCode = LeaseExpiredCode;

            await transitions.FinishAsync(
                job,
                JobStatus.RecoveryRequired,
                AttemptStatus.Abandoned,
                JobEventKind.LeaseExpired,
                now,
                cancellationToken);

            return true;
        }, cancellationToken);

    public Task<bool> ResolveRecoveryAsync(Guid jobId, bool retryIsSafe, CancellationToken cancellationToken) =>
        jobStore.ChangeAsync(jobId, (job, now) =>
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
                transitions.ScheduleRetry(job, TimeSpan.Zero, now);
            }
            else
            {
                job.Status = JobStatus.Failed;
                job.ErrorCode = UnsafeRetryCode;
            }

            job.FinishedAt = transitions.IsTerminal(job.Status) ? now : null;

            transitions.AddEvent(job, JobEventKind.RecoveryResolved, now);

            return Task.FromResult(true);
        }, cancellationToken);

    private static void ValidateLease(TimeSpan leaseDuration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseDuration, TimeSpan.Zero);
    }
}
