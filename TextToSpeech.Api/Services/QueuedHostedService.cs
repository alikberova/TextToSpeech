using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Api.Services;

internal sealed class QueuedHostedService(IBackgroundTaskQueue _taskQueue, ILogger<QueuedHostedService> _logger) : BackgroundService
{
    private const int MaxConcurrentTasks = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is starting.");

        // Wait for all active tasks to complete before stopping the service.
        await Task.WhenAll(Enumerable.Range(0, MaxConcurrentTasks).Select(_ => ConsumeAsync(stoppingToken)));

        _logger.LogInformation("Queued Hosted Service is stopping.");
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing work item.");
            }
        }
    }
}
