namespace TextToSpeech.Core.Models;

// Transitional execution input; durable source references belong to the storage stage.
public sealed record SpeechGenerationInput(
    Guid FileId,
    string OwnerId,
    string FileName,
    string FileText,
    string TtsApi,
    TtsRequestOptions Options);
