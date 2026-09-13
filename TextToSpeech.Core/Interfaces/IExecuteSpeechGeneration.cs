using TextToSpeech.Core.Models;
using TextToSpeech.Core.Jobs;

namespace TextToSpeech.Core.Interfaces;

public interface IExecuteSpeechGeneration
{
    Task ExecuteAsync(JobExecution execution, IProgress<ProgressReport> progress,
        CancellationToken cancellationToken);
}
