using Microsoft.Extensions.Options;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs;

namespace TextToSpeech.Worker;

public sealed class JobRunner(
    IServiceScopeFactory scopeFactory,
    JobHeartbeat heartbeat,
    IOptions<WorkerConfig> options,
    ILogger<JobRunner> logger)
{
    public const string ExecutionFailedCode = "job_execution_failed";

    public async Task<bool> RunAsync(JobDispatchMessage message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<WorkerJobReader>();
        var job = await reader.GetAsync(message.JobId, cancellationToken);

        if (job is null || job.Status is not (JobStatus.Pending or JobStatus.Running))
        {
            return true;
        }

        var handler = scope.ServiceProvider.GetKeyedService<IBackgroundJobHandler>(job.JobType);

        if (message.ContractVersion != JobDispatchMessage.CurrentVersion || handler?.InputVersion != job.InputVersion)
        {
            return false;
        }

        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();
        var execution = await jobs.TryStartAsync(job.Id, TimeSpan.FromSeconds(options.Value.LeaseSeconds),
            cancellationToken);

        if (execution is null)
        {
            // A live duplicate is harmless. An abandoned attempt requires reconciliation, never blind replay.
            await jobs.RecoverExpiredAsync(job.Id, cancellationToken);

            return true;
        }

        await ExecuteAsync(execution, handler, cancellationToken);

        return true;
    }

    private async Task ExecuteAsync(
        JobExecution execution,
        IBackgroundJobHandler handler,
        CancellationToken stoppingToken)
    {
        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var progress = new JobProgress();
        var renewal = heartbeat.RunAsync(execution, progress, executionCancellation, heartbeatCancellation.Token);

        try
        {
            logger.LogInformation("Starting job {JobId}, attempt {AttemptId}, correlation {CorrelationId}",
                execution.JobId, execution.AttemptId, execution.Job.CorrelationId);
            await handler.ExecuteAsync(execution, progress, executionCancellation.Token);
        }
        catch (OperationCanceledException) when (executionCancellation.IsCancellationRequested)
        {
            // Only persisted user intent can become Cancelled. Shutdown/lost lease needs recovery.
            using var scope = scopeFactory.CreateScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();

            await jobs.AcknowledgeCancellationAsync(execution.JobId, execution.AttemptId, CancellationToken.None);
            stoppingToken.ThrowIfCancellationRequested();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Execution failed for job {JobId}", execution.JobId);

            // Use a fresh context: the handler may have rolled back a result transaction.
            using var scope = scopeFactory.CreateScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();

            await jobs.FailAsync(execution.JobId, execution.AttemptId, ExecutionFailedCode, null, stoppingToken);
        }
        finally
        {
            await heartbeatCancellation.CancelAsync();
            await renewal;
        }
    }
}
