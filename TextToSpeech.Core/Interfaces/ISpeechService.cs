using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces;

public interface ISpeechService
{
    Task<MemoryStream> CreateSpeechSample(TtsRequestOptions request,
        string input,
        string ttsApi,
        string ownerId);
}
