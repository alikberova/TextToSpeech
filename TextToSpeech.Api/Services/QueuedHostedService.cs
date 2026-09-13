using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Api.Services;

internal sealed class QueuedHostedService(IBackgroundTaskQueue _taskQueue, ILogger<QueuedHostedService> _logger) : BackgroundService
{
    private readonly List<Task> _activeTasks = [];
    private readonly SemaphoreSlim _semaphore = new(100);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _semaphore.WaitAsync(stoppingToken); // Wait for an available slot
                    var task = workItem(stoppingToken);
                    lock (_activeTasks)
                    {
                        _activeTasks.Add(task);
                    }
                    await task;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing work item.");
                }
                finally
                {
                    _semaphore.Release();
                    lock (_activeTasks)
                    {
                        _activeTasks.RemoveAll(t => t.IsCompleted);
                    }
                }
            }, stoppingToken);
        }

        _logger.LogInformation("Queued Hosted Service is stopping.");

        // Wait for all active tasks to complete before stopping the service
        await Task.WhenAll(_activeTasks);
    }
}