namespace TextToSpeech.Infra.Dto.Narakeet;

public sealed record NarakeetVoiceResult
{
    public string Name { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string LanguageCode { get; init; } = string.Empty;
    public List<string> Styles { get; init; } = [];
}
