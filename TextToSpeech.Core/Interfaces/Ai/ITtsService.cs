using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces.Ai;

public interface ITtsService
{
    int MaxLengthPerApiRequest { get; init; }

    Task<ReadOnlyMemory<byte>[]> RequestSpeechChunksAsync(List<string> textChunks,
        Guid fileId,
        TtsRequestOptions ttsRequest,
        IProgress<ProgressReport> progressCallback,
        CancellationToken cancellationToken);

    Task<ReadOnlyMemory<byte>> RequestSpeechSample(string text,
        TtsRequestOptions ttsRequest,
        CancellationToken cancellationToken = default);

    Task<List<Voice>?> GetVoices();
}