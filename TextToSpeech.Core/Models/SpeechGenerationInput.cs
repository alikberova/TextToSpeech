namespace TextToSpeech.Core.Models;

// Materialized speech input loaded from durable request metadata and artifact storage; never a queue message.
public sealed record SpeechGenerationInput(
    Guid FileId,
    string OwnerId,
    string FileName,
    string FileText,
    string TtsApi,
    TtsRequestOptions Options);
