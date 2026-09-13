namespace TextToSpeech.Core.Jobs;

// InputId identifies immutable domain data. Neither input nor result bytes belong in Jobs.
public sealed record JobSubmission(
    string JobType,
    int InputVersion,
    Guid InputId,
    string OwnerId,
    string IdempotencyKey,
    string InputFingerprint,
    Guid CorrelationId,
    int MaxAttempts = 3)
{
    public const int MaxOwnerIdLength = 100;
    public const int MaxJobTypeLength = 100;
    public const int MaxIdempotencyKeyLength = 200;
}

public sealed record JobAcceptance(
    Guid JobId,
    Guid InputId,
    bool Created);
