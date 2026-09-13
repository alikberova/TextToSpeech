namespace TextToSpeech.Infra.Config;

public sealed class ElasticsearchConfig
{
    public string Url { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
