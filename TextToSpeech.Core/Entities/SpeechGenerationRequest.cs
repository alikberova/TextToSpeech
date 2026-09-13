namespace TextToSpeech.Core.Entities;

public sealed class SpeechGenerationRequest
{
    public Guid Id { get; init; }
    public Guid JobId { get; init; }
    public required string OwnerId { get; init; }
    public required string FileName { get; init; }
    public required string Provider { get; init; }
    public int Version { get; init; }
    public Guid SourceId { get; init; }
    public Guid TextId { get; init; }
    public required string OptionsJson { get; init; }
}
