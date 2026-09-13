using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces;

public interface IExecuteSpeechGeneration
{
    Task ExecuteAsync(SpeechGenerationInput input, IProgress<ProgressReport> progress,
        CancellationToken cancellationToken);
}
