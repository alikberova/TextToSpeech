namespace TextToSpeech.Core.Entities;

public sealed class TtsApi
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<AudioFile> AudioFiles { get; set; } = [];
}
