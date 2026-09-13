using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Services.FileProcessing;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

public sealed class ExecuteSpeechGeneration(
    ITextProcessingService textProcessingService,
    ITtsServiceFactory ttsServiceFactory,
    IMetaDataService metaDataService,
    IAudioFileRepository audioFileRepository,
    ISpeechGenerationRequests requests,
    IBackgroundJobs jobs,
    AppDbContext context) : IExecuteSpeechGeneration
{
    public async Task ExecuteAsync(JobExecution execution, IProgress<ProgressReport> progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateExecution(execution);

        var input = await requests.LoadAsync(execution.Job.InputId, cancellationToken);

        if (input.OwnerId != execution.OwnerId)
        {
            throw new InvalidOperationException("Speech request does not belong to the job owner.");
        }

        var request = input.Options;
        var ttsService = ttsServiceFactory.Get(input.TtsApi);
        var textChunks = textProcessingService.SplitTextIfGreaterThan(
            input.FileText,
            ttsService.MaxLengthPerApiRequest);
        var bytesCollection = await ttsService.RequestSpeechChunksAsync(textChunks,
            input.FileId, request, progress, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var bytes = AudioFileService.ConcatenateRawAudioChunks(bytesCollection, request.ResponseFormat.ToString());

        if (request.ResponseFormat != SpeechResponseFormat.Pcm)
        {
            bytes = await metaDataService.AddMetaData(bytes, request.ResponseFormat.ToString(), input.FileName);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var audioFile = AudioFileBuilder.Create(AudioType.Full, input.FileText, request,
            input.OwnerId,
            Shared.TtsApis.Single(kv => kv.Key.Equals(input.TtsApi, StringComparison.OrdinalIgnoreCase)).Value,
            input.FileName, input.FileId);
        audioFile.Status = Status.Completed;

        // Once persistence starts, its outcome determines completion, even if cancellation arrives.
        await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

        await audioFileRepository.Add(audioFile, bytes);

        if (!await jobs.CompleteAsync(execution.JobId, execution.AttemptId, input.FileId, CancellationToken.None))
        {
            throw new InvalidOperationException("The job no longer owns this execution attempt.");
        }

        await transaction.CommitAsync(CancellationToken.None);
    }

    private static void ValidateExecution(JobExecution execution)
    {
        if (execution.Job.JobType != SpeechGenerationRequests.JobType ||
            execution.Job.InputVersion != SpeechGenerationRequests.InputVersion)
        {
            throw new NotSupportedException("Unsupported speech job contract.");
        }
    }
}
