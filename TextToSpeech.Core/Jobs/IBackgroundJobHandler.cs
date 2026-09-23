namespace TextToSpeech.Core.Jobs;

public interface IBackgroundJobHandler
{
    int InputVersion { get; }

    Task ExecuteAsync(JobExecution execution, IProgress<int> progress, CancellationToken cancellationToken);
}
