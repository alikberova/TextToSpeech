using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Core.Entities;

public sealed class AudioFile
{
    public Guid Id { get; init; }
    /// <summary>
    /// ClaimTypes.NameIdentifier
    /// </summary>
    public string OwnerId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public Guid ContentId { get; set; }
    public DateTime CreatedAt { get; init; }
    public string Description { get; init; } = string.Empty;
    public Status Status { get; set; }
    public string Hash { get; init; } = string.Empty;
    /// <summary>
    /// Provider Voice Id
    /// </summary>
    public string Voice { get; init; } = string.Empty;
    public string? LanguageCode { get; init; }
    public double Speed { get; init; }
    public AudioType Type { get; init; }

    public Guid? TtsApiId { get; init; }
    public TtsApi? TtsApi { get; init; }

    public override string ToString()
    {
        return $"{Id}: {Description}";
    }
}
