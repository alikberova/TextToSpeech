using Microsoft.Extensions.Options;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs;

namespace TextToSpeech.Worker;

public sealed class JobRecoveryService(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerConfig> options,
    ILogger<JobRecoveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.RecoveryIntervalSeconds));

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var reader = scope.ServiceProvider.GetRequiredService<WorkerJobReader>();
                var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();
                var expired = await reader.GetExpiredAsync(stoppingToken);

                foreach (var jobId in expired)
                {
                    if (await jobs.RecoverExpiredAsync(jobId, stoppingToken))
                    {
                        logger.LogWarning("Abandoned job {JobId} requires reconciliation", jobId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not recover abandoned jobs");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
