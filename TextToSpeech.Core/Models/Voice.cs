using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Core.Models;

public sealed class Voice
{
    public required string Name { get; init; }
    public required string ProviderVoiceId { get; init; }
    public Language? Language { get; init; }
    public VoiceQualityTier QualityTier { get; init; }
}

public sealed record Language(string Name, string LanguageCode);