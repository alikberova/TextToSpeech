using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra.Jobs;

public sealed class BackgroundJobStateTransitions(AppDbContext context)
{
    internal bool IsTerminal(JobStatus status) =>
        status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled;

    internal bool OwnsExecution(BackgroundJob job, Guid attemptId, DateTimeOffset now) =>
        job.Status == JobStatus.Running &&
        job.CurrentAttemptId == attemptId &&
        job.LeaseExpiresAt > now;

    internal void AddEvent(BackgroundJob job, JobEventKind eventKind, DateTimeOffset now)
    {
        context.AddEvent(job, eventKind, now);
    }

    internal async Task FinishAsync(
        BackgroundJob job,
        JobStatus status,
        AttemptStatus attemptStatus,
        JobEventKind eventKind,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (job.CurrentAttemptId.HasValue)
        {
            var attempt = await context.BackgroundJobAttempts
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

    internal void ScheduleRetry(BackgroundJob job, TimeSpan delay, DateTimeOffset now)
    {
        job.Status = JobStatus.Pending;
        job.FinishedAt = null;
        job.AvailableAt = now + delay;

        context.AddEvent(job, JobEventKind.RetryScheduled, now);
        context.AddDispatch(job, now);
    }
}
