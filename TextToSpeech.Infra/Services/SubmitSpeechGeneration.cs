using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

public sealed class SubmitSpeechGeneration(
    IFileProcessorFactory fileProcessorFactory,
    IAudioFileRepository audioFileRepository,
    ISpeechGenerationDispatcher dispatcher,
    ISpeechGenerationRequests requests,
    IBackgroundJobs jobs,
    ISpeechGenerationNotifications notifications) : ISubmitSpeechGeneration
{
    public async Task<Guid> SubmitAsync(TtsRequestOptions request, byte[] fileBytes, string fileName,
        string ttsApi, string ownerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var providerId = Shared.TtsApis.Single(kv => kv.Key.Equals(ttsApi, StringComparison.OrdinalIgnoreCase)).Value;
        var fileProcessor = fileProcessorFactory.GetProcessor(Path.GetExtension(fileName)) ??
            throw new NotSupportedException("File type not supported");
        var fileText = await fileProcessor.ExtractTextAsync(fileBytes);
        cancellationToken.ThrowIfCancellationRequested();

        var hash = AudioFileBuilder.GenerateHash(fileText, request, AudioType.Full);
        var existingAudio = await audioFileRepository.GetCompletedByHash(hash, ownerId, providerId);

        if (existingAudio is not null)
        {
            _ = notifications.PublishAsync(existingAudio.Value, ownerId, Status.Completed);
            return existingAudio.Value;
        }

        var provider = Shared.TtsApis.Single(x => x.Value == providerId).Key;
        var input = new SpeechGenerationInput(Guid.NewGuid(), ownerId, fileName, fileText, provider, request);
        var accepted = await requests.AcceptAsync(input, fileBytes, cancellationToken);

        if (accepted.Created)
        {
            await dispatcher.DispatchAsync(accepted.JobId, accepted.InputId, ownerId, cancellationToken);
        }
        else
        {
            await HandleAcceptedJobAsync(accepted, ownerId, cancellationToken);
        }

        return accepted.InputId;
    }

    private async Task HandleAcceptedJobAsync(
        JobAcceptance accepted,
        string ownerId,
        CancellationToken cancellationToken)
    {
        var job = await jobs.GetAsync(accepted.JobId, ownerId, cancellationToken)
            ?? throw new InvalidOperationException("Accepted speech job is missing.");

        if (job.Status == JobStatus.Pending)
        {
            await dispatcher.DispatchAsync(accepted.JobId, accepted.InputId, ownerId, cancellationToken);

            return;
        }

        var status = job.Status switch
        {
            JobStatus.Completed => Status.Completed,
            JobStatus.Cancelled => Status.Canceled,
            JobStatus.Failed or JobStatus.RecoveryRequired => Status.Failed,
            JobStatus.Running => Status.Processing,
            _ => Status.Created
        };

        _ = notifications.PublishAsync(accepted.InputId, ownerId, status, job.Progress, job.ErrorCode);
    }
}
