using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces;

public interface ISpeechService
{
    Task<Guid> GetOrInitiateSpeech(TtsRequestOptions request,
        byte[] fileBytes,
        string fileName,
        string ttsApi,
        string ownerId);
    Task<MemoryStream> CreateSpeechSample(TtsRequestOptions request,
        string input,
        string ttsApi,
        string ownerId);
    Task ProcessSpeechAsync(TtsRequestOptions request,
        string fileText,
        string fileName,
        string ttsApi,
        Guid fileId,
        string ownerId,
        CancellationToken cancellationToken);
}
