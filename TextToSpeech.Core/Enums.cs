namespace TextToSpeech.Core;

public sealed class Enums
{
    public enum Status
    {
        Created,
        Processing,
        Completed,
        Failed,
        Canceled
    }

    public enum AudioType
    {
        Unspecified,
        Sample,
        Full
    }

    public enum VoiceQualityTier
    {
        Standard,
        Premium
    }
}
