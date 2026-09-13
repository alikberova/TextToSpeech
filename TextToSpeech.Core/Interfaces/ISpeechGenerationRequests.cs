using TextToSpeech.Core.Jobs;
using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces;

public interface ISpeechGenerationRequests
{
    Task<JobAcceptance> AcceptAsync(SpeechGenerationInput input, byte[] source, CancellationToken cancellationToken);
    Task<SpeechGenerationInput> LoadAsync(Guid fileId, CancellationToken cancellationToken);
    Task<Guid?> GetJobIdAsync(Guid fileId, string ownerId, CancellationToken cancellationToken);
}
