using TextToSpeech.Core.Jobs;

namespace TextToSpeech.Infra.Jobs.Persistence;

internal enum JobEventKind
{
    Submitted,
    Started,
    CancellationRequested,
    Completed,
    Failed,
    Cancelled,
    RetryScheduled,
    LeaseExpired,
    RecoveryResolved
}

internal sealed class BackgroundJobEvent
{
    public long Id { get; set; }
    public Guid JobId { get; set; }
    public Guid? AttemptId { get; set; }
    public JobEventKind Kind { get; set; }
    public JobStatus Status { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? ErrorCode { get; set; }
}
