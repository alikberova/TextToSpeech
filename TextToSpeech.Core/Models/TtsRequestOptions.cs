namespace TextToSpeech.Core.Models;

public sealed record TtsRequestOptions
{
    public string? Model { get; init; }
    public required Voice Voice { get; init; }
    public required double Speed { get; init; }
    public required SpeechResponseFormat ResponseFormat { get; init; }
}