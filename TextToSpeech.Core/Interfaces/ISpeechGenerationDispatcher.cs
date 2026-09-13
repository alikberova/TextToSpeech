using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces;

public interface ISpeechGenerationDispatcher
{
    Task DispatchAsync(SpeechGenerationInput input, CancellationToken cancellationToken);
}
