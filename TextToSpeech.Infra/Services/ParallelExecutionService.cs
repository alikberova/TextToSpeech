using Microsoft.Extensions.Logging;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.Services;

public sealed class ParallelExecutionService : IParallelExecutionService
{
    private readonly ILogger<ParallelExecutionService> _logger;

    public ParallelExecutionService(ILogger<ParallelExecutionService> logger)
    {
        _logger = logger;
    }

    public async Task RunTasksFromItems<T>(IReadOnlyList<T> items, int maxParallel, Func<T, int, Task> action,
        CancellationToken cancellationToken)
    {
        using var gate = new SemaphoreSlim(maxParallel);

        _logger.LogInformation(
            "Initialized SemaphoreSlim with max parallel chunk limit {Limit}",
            maxParallel);

        var tasks = items.Select((item, index) =>
        {
            return Task.Run(async () =>
            {
                await gate.WaitAsync(cancellationToken);

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await action(item, index);
                }
                finally
                {
                    gate.Release();
                }
            }, cancellationToken);
        }).ToList();

        await Task.WhenAll(tasks);
    }
}
