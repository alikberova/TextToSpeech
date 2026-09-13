using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Interfaces;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

// Temporary host adapter. RabbitMQ will replace this queue; persisted Jobs already own execution.
public sealed class QueuedSpeechGenerationDispatcher(
    IBackgroundTaskQueue backgroundTaskQueue,
    ICancellationRegistry cancellationRegistry,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QueuedSpeechGenerationDispatcher> logger) : ISpeechGenerationDispatcher
{
    private const string GenerationFailedCode = "speech_generation_failed";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RenewalInterval = TimeSpan.FromSeconds(10);

    public Task DispatchAsync(Guid jobId, Guid fileId, string ownerId, CancellationToken cancellationToken) =>
        backgroundTaskQueue.QueueBackgroundWorkItem(
            token => ExecuteAsync(jobId, fileId, ownerId, token), cancellationToken);

    private async Task ExecuteAsync(Guid jobId, Guid fileId, string ownerId, CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();
        var notifications = scope.ServiceProvider.GetRequiredService<ISpeechGenerationNotifications>();
        var execution = await jobs.TryStartAsync(jobId, LeaseDuration, stoppingToken);

        if (execution is null)
        {
            await PublishTerminalStatusAsync(jobId, fileId, ownerId, notifications);
            return;
        }

        await ExecuteClaimedAsync(jobId, fileId, ownerId, jobs, notifications, execution, stoppingToken);
    }

    private async Task ExecuteClaimedAsync(
        Guid jobId,
        Guid fileId,
        string ownerId,
        IBackgroundJobs jobs,
        ISpeechGenerationNotifications notifications,
        JobExecution execution,
        CancellationToken stoppingToken)
    {
        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        // Only the claimed attempt registers cancellation; duplicate queue entries must not replace it.
        cancellationRegistry.AddTask(fileId, ownerId, executionCancellation);

        try
        {
            using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var progress = new SpeechGenerationProgress();
            var publishing = progress.PublishAsync(fileId, ownerId, notifications);
            var heartbeat = RenewLeaseAsync(execution, executionCancellation, heartbeatCancellation.Token);

            try
            {
                await RunExecutorAsync(fileId, ownerId, notifications, execution, progress, executionCancellation.Token);
            }
            catch (OperationCanceledException) when (executionCancellation.IsCancellationRequested)
            {
                // Only persisted user intent can become Cancelled. Shutdown/lost lease needs recovery.
                await jobs.AcknowledgeCancellationAsync(jobId, execution.AttemptId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Speech generation failed for {fileId}", fileId);
                await jobs.FailAsync(jobId, execution.AttemptId, GenerationFailedCode, null, CancellationToken.None);
            }
            finally
            {
                await heartbeatCancellation.CancelAsync();
                await heartbeat;
                progress.Complete();
                await publishing;
                await PublishTerminalStatusAsync(jobId, fileId, ownerId, notifications);
            }
        }
        finally
        {
            await cancellationRegistry.CompleteTaskAsync(fileId);
        }
    }

    private async Task RunExecutorAsync(
        Guid fileId,
        string ownerId,
        ISpeechGenerationNotifications notifications,
        JobExecution execution,
        SpeechGenerationProgress progress,
        CancellationToken cancellationToken)
    {
        await notifications.PublishAsync(fileId, ownerId, Status.Created);
        cancellationToken.ThrowIfCancellationRequested();
        await notifications.PublishAsync(fileId, ownerId, Status.Processing);

        // A failed result transaction must not leave tracked entities in the lifecycle context.
        using var executionScope = serviceScopeFactory.CreateScope();
        var executor = executionScope.ServiceProvider.GetRequiredService<IExecuteSpeechGeneration>();

        await executor.ExecuteAsync(execution, progress, cancellationToken);
    }

    private async Task RenewLeaseAsync(JobExecution execution, CancellationTokenSource executionCancellation,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(RenewalInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                using var scope = serviceScopeFactory.CreateScope();
                var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();
                var renewed = await jobs.RenewAsync(
                    execution.JobId,
                    execution.AttemptId,
                    LeaseDuration,
                    0,
                    cancellationToken);
                var job = await jobs.GetAsync(execution.JobId, execution.OwnerId, cancellationToken);

                if (!renewed ||
                    job is null ||
                    job.CancellationRequested)
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not renew speech job {jobId}", execution.JobId);
            await executionCancellation.CancelAsync();
        }
    }

    private async Task PublishTerminalStatusAsync(Guid jobId, Guid fileId, string ownerId,
        ISpeechGenerationNotifications notifications)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();
        var job = await jobs.GetAsync(jobId, ownerId, CancellationToken.None);
        Status? status = job?.Status switch
        {
            JobStatus.Completed => Status.Completed,
            JobStatus.Failed => Status.Failed,
            JobStatus.Cancelled => Status.Canceled,
            _ => null
        };

        if (status.HasValue)
        {
            await notifications.PublishAsync(fileId, ownerId, status.Value, errorMessage: job!.ErrorCode);
        }
    }
}
