namespace TextToSpeech.Infra.Interfaces;

public interface IParallelExecutionService
{
    Task RunTasksFromItems<T>(IReadOnlyList<T> items, int maxParallel, Func<T, int, Task> action,
        CancellationToken cancellationToken);
}