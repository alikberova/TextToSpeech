namespace TextToSpeech.Core.Jobs;

public interface IBackgroundJobs
{
    Task<JobSnapshot?> GetAsync(Guid jobId, string ownerId, CancellationToken cancellationToken);
    Task<bool> RequestCancellationAsync(Guid jobId, string ownerId, CancellationToken cancellationToken);

    // Execution methods are for trusted workers, never directly exposed as client endpoints.
    Task<JobExecution?> TryStartAsync(Guid jobId, TimeSpan leaseDuration, CancellationToken cancellationToken);

    Task<bool> RenewAsync(
        Guid jobId,
        Guid attemptId,
        TimeSpan leaseDuration,
        int progress,
        CancellationToken cancellationToken);

    // Requires the caller's transaction containing result metadata; the caller commits or rolls it back.
    Task<bool> CompleteAsync(Guid jobId, Guid attemptId, Guid resultId, CancellationToken cancellationToken);

    Task<bool> FailAsync(
        Guid jobId,
        Guid attemptId,
        string errorCode,
        TimeSpan? safeRetryDelay,
        CancellationToken cancellationToken);

    Task<bool> AcknowledgeCancellationAsync(Guid jobId, Guid attemptId, CancellationToken cancellationToken);
    Task<bool> RecoverExpiredAsync(Guid jobId, CancellationToken cancellationToken);

    // Call only after domain-specific reconciliation proves another provider call is safe.
    Task<bool> ResolveRecoveryAsync(Guid jobId, bool retryIsSafe, CancellationToken cancellationToken);
}
