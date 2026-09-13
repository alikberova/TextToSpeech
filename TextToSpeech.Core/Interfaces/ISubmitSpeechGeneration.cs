using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces;

public interface ISubmitSpeechGeneration
{
    Task<Guid> SubmitAsync(TtsRequestOptions request, byte[] fileBytes, string fileName,
        string ttsApi, string ownerId, CancellationToken cancellationToken);
}
