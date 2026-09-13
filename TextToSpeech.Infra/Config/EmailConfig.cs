namespace TextToSpeech.Infra.Config;

public sealed class EmailConfig
{
    public string EmailTo { get; init; } = string.Empty;
    public string EmailFrom { get; init; } = string.Empty;
    public string EmailFromPassword { get; init; } = string.Empty;
}
