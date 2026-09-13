namespace TextToSpeech.Infra.Interfaces;

public interface IBackgroundTaskQueue
{
    Task QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, CancellationToken cancellationToken);
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
