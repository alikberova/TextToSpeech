using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Core.Models;

namespace TextToSpeech.Infra.Services;

public sealed class SpeechGenerationJobHandler(IExecuteSpeechGeneration executor) : IBackgroundJobHandler
{
    public int InputVersion => SpeechGenerationRequests.InputVersion;

    public Task ExecuteAsync(JobExecution execution, IProgress<int> progress, CancellationToken cancellationToken) =>
        executor.ExecuteAsync(execution, new SpeechProgress(progress), cancellationToken);

    private sealed class SpeechProgress(IProgress<int> progress) : IProgress<ProgressReport>
    {
        public void Report(ProgressReport value) => progress.Report(value.ProgressPercentage);
    }
}
