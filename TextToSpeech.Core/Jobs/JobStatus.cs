namespace TextToSpeech.Core.Jobs;

public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    RecoveryRequired
}
