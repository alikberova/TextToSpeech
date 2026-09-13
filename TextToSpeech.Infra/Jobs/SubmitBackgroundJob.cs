using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra.Jobs;

public sealed class SubmitBackgroundJob(AppDbContext context) : ISubmitBackgroundJob
{
    public Task<JobAcceptance> SubmitAsync(JobSubmission submission, CancellationToken cancellationToken)
    {
        Validate(submission);
        return context.InTransactionAsync(async () =>
        {
            await context.LockSubmissionAsync(submission, cancellationToken);

            var existing = await context.BackgroundJobs
                .AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.OwnerId == submission.OwnerId &&
                    x.JobType == submission.JobType &&
                    x.IdempotencyKey == submission.IdempotencyKey, cancellationToken);

            if (existing is not null)
            {
                if (existing.InputFingerprint != submission.InputFingerprint ||
                    existing.InputVersion != submission.InputVersion)
                {
                    throw new InvalidOperationException("An idempotency key cannot be reused for different input.");
                }

                return new JobAcceptance(existing.Id, existing.InputId, false);
            }

            var now = await context.GetDatabaseTimeAsync(cancellationToken);
            var job = new BackgroundJob
            {
                Id = Guid.NewGuid(),
                JobType = submission.JobType,
                InputVersion = submission.InputVersion,
                InputId = submission.InputId,
                OwnerId = submission.OwnerId,
                IdempotencyKey = submission.IdempotencyKey,
                InputFingerprint = submission.InputFingerprint,
                CorrelationId = submission.CorrelationId,
                MaxAttempts = submission.MaxAttempts,
                Status = JobStatus.Pending,
                CreatedAt = now,
                AvailableAt = now
            };

            context.BackgroundJobs.Add(job);
            context.AddEvent(job, JobEventKind.Submitted, now);
            context.AddDispatch(job, now);
            await context.SaveChangesAsync(cancellationToken);

            return new JobAcceptance(job.Id, job.InputId, true);
        }, cancellationToken);
    }

    private static void Validate(JobSubmission submission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(submission.JobType);
        ArgumentException.ThrowIfNullOrWhiteSpace(submission.OwnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(submission.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(submission.InputFingerprint);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(submission.OwnerId.Length, JobSubmission.MaxOwnerIdLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(submission.JobType.Length, JobSubmission.MaxJobTypeLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(submission.IdempotencyKey.Length,
            JobSubmission.MaxIdempotencyKeyLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(submission.InputVersion, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(submission.MaxAttempts, 1);
    }
}
