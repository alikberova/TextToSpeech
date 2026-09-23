using Microsoft.Extensions.Options;
using TextToSpeech.Core.Jobs;

namespace TextToSpeech.Worker;

public sealed class JobHeartbeat(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerConfig> options,
    ILogger<JobHeartbeat> logger)
{
    internal async Task RunAsync(JobExecution execution, JobProgress progress,
        CancellationTokenSource executionCancellation, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.HeartbeatSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                using var scope = scopeFactory.CreateScope();
                var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();
                var renewed = await jobs.RenewAsync(execution.JobId, execution.AttemptId,
                    TimeSpan.FromSeconds(options.Value.LeaseSeconds), progress.Value, cancellationToken);
                var job = await jobs.GetAsync(execution.JobId, execution.OwnerId, cancellationToken);

                if (!renewed || job is null || job.CancellationRequested)
                {
                    await executionCancellation.CancelAsync();

                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Execution finished or the host is stopping.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not extend processing lock for job. {JobId}", execution.JobId);
            await executionCancellation.CancelAsync();
        }
    }
}
