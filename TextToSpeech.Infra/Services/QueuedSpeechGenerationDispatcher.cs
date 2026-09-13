using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Interfaces;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

// Temporary host adapter. Replace it with durable dispatch when introducing the Jobs module.
public sealed class QueuedSpeechGenerationDispatcher(
    IBackgroundTaskQueue backgroundTaskQueue,
    ICancellationRegistry cancellationRegistry,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QueuedSpeechGenerationDispatcher> logger) : ISpeechGenerationDispatcher
{
    public async Task DispatchAsync(SpeechGenerationInput input, CancellationToken cancellationToken)
    {
        var cancellationSource = new CancellationTokenSource();
        cancellationRegistry.AddTask(input.FileId, input.OwnerId, cancellationSource);
        try
        {
            await backgroundTaskQueue.QueueBackgroundWorkItem(
                token => ExecuteAsync(input, cancellationSource.Token, token), cancellationToken);
        }
        catch
        {
            await cancellationRegistry.CompleteTaskAsync(input.FileId);
            throw;
        }
    }

    private async Task ExecuteAsync(SpeechGenerationInput input, CancellationToken requestCancellation,
        CancellationToken stoppingToken)
    {
        try
        {
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, requestCancellation);
            using var scope = serviceScopeFactory.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IExecuteSpeechGeneration>();
            var notifications = scope.ServiceProvider.GetRequiredService<ISpeechGenerationNotifications>();
            var progress = new SpeechGenerationProgress();
            var publishing = progress.PublishAsync(input, notifications);
            var status = Status.Completed;
            string? errorMessage = null;

            try
            {
                await notifications.PublishAsync(input.FileId, input.OwnerId, Status.Created);
                linkedSource.Token.ThrowIfCancellationRequested();
                await notifications.PublishAsync(input.FileId, input.OwnerId, Status.Processing);
                await executor.ExecuteAsync(input, progress, linkedSource.Token);
            }
            catch (OperationCanceledException) when (linkedSource.IsCancellationRequested)
            {
                status = Status.Canceled;
            }
            catch (Exception ex)
            {
                status = Status.Failed;
                errorMessage = ex.Message;
                logger.LogError(ex, "Speech generation failed for {fileId}", input.FileId);
            }
            finally
            {
                progress.Complete();
                await publishing;
                await notifications.PublishAsync(input.FileId, input.OwnerId, status, errorMessage: errorMessage);
            }
        }
        finally
        {
            await cancellationRegistry.CompleteTaskAsync(input.FileId);
        }
    }
}
