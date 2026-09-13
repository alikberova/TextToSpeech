using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Services.FileProcessing;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

public sealed class ExecuteSpeechGeneration(
    ITextProcessingService textProcessingService,
    ITtsServiceFactory ttsServiceFactory,
    IMetaDataService metaDataService,
    IAudioFileRepository audioFileRepository) : IExecuteSpeechGeneration
{
    public async Task ExecuteAsync(SpeechGenerationInput input, IProgress<ProgressReport> progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = input.Options;
        var ttsService = ttsServiceFactory.Get(input.TtsApi);
        var textChunks = textProcessingService.SplitTextIfGreaterThan(input.FileText, ttsService.MaxLengthPerApiRequest);
        var bytesCollection = await ttsService.RequestSpeechChunksAsync(textChunks,
            input.FileId, request, progress, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var bytes = AudioFileService.ConcatenateRawAudioChunks(bytesCollection, request.ResponseFormat.ToString());
        if (request.ResponseFormat != SpeechResponseFormat.Pcm)
        {
            bytes = await metaDataService.AddMetaData(bytes, request.ResponseFormat.ToString(), input.FileName);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var audioFile = AudioFileBuilder.Create(bytes, AudioType.Full, input.FileText, request,
            input.OwnerId,
            Shared.TtsApis.Single(kv => kv.Key.Equals(input.TtsApi, StringComparison.OrdinalIgnoreCase)).Value,
            input.FileName, input.FileId);
        audioFile.Status = Status.Completed;

        // Once persistence starts, its outcome determines completion, even if cancellation arrives.
        await audioFileRepository.Add(audioFile);
    }
}
