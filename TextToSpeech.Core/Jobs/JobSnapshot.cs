namespace TextToSpeech.Core.Jobs;

public sealed record JobSnapshot(
    Guid Id,
    string JobType,
    int InputVersion,
    Guid InputId,
    Guid CorrelationId,
    JobStatus Status,
    int Progress,
    bool CancellationRequested,
    Guid? ResultId,
    string? ErrorCode);

public sealed record JobExecution(
    Guid JobId,
    Guid AttemptId,
    string OwnerId,
    JobSnapshot Job);
