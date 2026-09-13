using TextToSpeech.Core.Jobs;

namespace TextToSpeech.Infra.Jobs.Persistence;

internal sealed class BackgroundJob
{
    public Guid Id { get; set; }
    public required string JobType { get; set; }
    public int InputVersion { get; set; }
    public Guid InputId { get; set; }
    public required string OwnerId { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string InputFingerprint { get; set; }
    public Guid CorrelationId { get; set; }
    public JobStatus Status { get; set; }
    public int Progress { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public Guid? CurrentAttemptId { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public DateTimeOffset? CancellationRequestedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public Guid? ResultId { get; set; }
    public string? ErrorCode { get; set; }

    public JobSnapshot Snapshot() => new(
        Id,
        JobType,
        InputVersion,
        InputId,
        CorrelationId,
        Status,
        Progress,
        CancellationRequestedAt.HasValue,
        ResultId,
        ErrorCode);
}
