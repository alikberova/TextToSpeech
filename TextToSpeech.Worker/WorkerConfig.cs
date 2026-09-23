namespace TextToSpeech.Worker;

public sealed class WorkerConfig
{
    public const string SectionName = "Worker";

    public int Concurrency { get; init; }
    public int LeaseSeconds { get; init; }
    public int HeartbeatSeconds { get; init; }
    public int RecoveryIntervalSeconds { get; init; }
}
