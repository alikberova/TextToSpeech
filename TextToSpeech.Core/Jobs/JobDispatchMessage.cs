namespace TextToSpeech.Core.Jobs;

public sealed record JobDispatchMessage(
    Guid JobId,
    int ContractVersion,
    Guid CorrelationId)
{
    public const int CurrentVersion = 1;
}
