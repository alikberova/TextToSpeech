namespace TextToSpeech.Infra.Jobs.Persistence;

internal enum AttemptStatus
{
    Running,
    Completed,
    Failed,
    Cancelled,
    Abandoned
}

internal sealed class BackgroundJobAttempt
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public int Number { get; set; }
    public AttemptStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? ErrorCode { get; set; }
}
