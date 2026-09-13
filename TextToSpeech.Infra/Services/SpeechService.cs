using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TextToSpeech.Core.Entities;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Services.FileProcessing;
using TextToSpeech.Infra.SignalR;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

public sealed class SpeechService(ITextProcessingService _textFileService,
    ITtsServiceFactory _ttsServiceFactory,
    IFileProcessorFactory _fileProcessorFactory,
    IHubContext<AudioHub> _hubContext,
    ILogger<SpeechService> _logger,
    IMetaDataService _metaDataService,
    IAudioFileRepository _audioFileRepository,
    ITaskManager _taskManager,
    IBackgroundTaskQueue _backgroundTaskQueue,
    IServiceScopeFactory _serviceScopeFactory,
    IRedisCacheProvider _redisCacheProvider) : ISpeechService
{
    private readonly ConcurrentDictionary<Guid, int> _lastProgressDictionary = new();
    private const int StatusUpdateDelayMs = 200; // todo not stable

    /// <summary>
    /// Process the speech asynchronously without waiting for it to complete to return the ID immediately to the client
    /// </summary>
    /// <param name="request"></param>
    /// <returns>ID of the newly created audio file</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Guid> GetOrInitiateSpeech(TtsRequestOptions request, byte[] fileBytes, string fileName,
        string ttsApi, string ownerId)
    {
        var fileText = await ExtractText(fileBytes, fileName);

        var hash = AudioFileBuilder.GenerateHash(fileText, request, AudioType.Full);

        var audioFileId = await _redisCacheProvider.Get<Guid?>(hash)
            ?? (await _audioFileRepository.GetByHash(hash))?.Id;

        if (audioFileId is not null)
        {
            _ = UpdateAudioStatus(audioFileId.Value, Status.Completed.ToString(), ownerId, delayMs: StatusUpdateDelayMs);
            _logger.LogInformation("Found existing audio for {AudioFileId}", audioFileId);

            return audioFileId.Value;
        }

        audioFileId = Guid.NewGuid();

        var cts = new CancellationTokenSource();
        _taskManager.AddTask(audioFileId.Value, cts);

        _ = UpdateAudioStatus(audioFileId.Value, Status.Created.ToString(), ownerId, delayMs: StatusUpdateDelayMs);

        _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
            using var scope = _serviceScopeFactory.CreateScope();
            var speechService = scope.ServiceProvider.GetRequiredService<ISpeechService>();
            await speechService.ProcessSpeechAsync(request, fileText, fileName, ttsApi,
                audioFileId.Value, ownerId, linkedCts.Token);
        });

        _logger.LogInformation("Initializing TTS for {AudioFileId}", audioFileId);

        return audioFileId.Value;
    }

    public async Task ProcessSpeechAsync(TtsRequestOptions request,
        string fileText,
        string fileName,
        string ttsApi,
        Guid fileId,
        string ownerId,
        CancellationToken cancellationToken)
    {
        AudioFile? audioFile = null;
        var finalStatus = Status.Processing;
        string? errorMessage = null;
        try
        {
            _logger.LogInformation("Processing speech for {FileId}", fileId);

            audioFile = AudioFileBuilder.Create([],
                AudioType.Full,
                fileText,
                request,
                ownerId,
                Shared.TtsApis.Single(kv => kv.Key.Equals(ttsApi, StringComparison.OrdinalIgnoreCase)).Value,
                fileName,
                fileId
            );

            finalStatus = Status.Processing;

            await UpdateAudioStatus(fileId, finalStatus.ToString(), ownerId);

            var ttsService = _ttsServiceFactory.Get(ttsApi);

            var textChunks = _textFileService.SplitTextIfGreaterThan(fileText, ttsService.MaxLengthPerApiRequest);

            var progress = new Progress<ProgressReport>();

            progress.ProgressChanged += async (_, report) =>
                await UpdateStatusAndProgress(fileId, report, finalStatus, ownerId);

            var bytesCollection = await ttsService.RequestSpeechChunksAsync(textChunks,
                fileId,
                request,
                progress,
                cancellationToken);

            var bytes = AudioFileService.ConcatenateRawAudioChunks(bytesCollection, request.ResponseFormat.ToString());

            if (request.ResponseFormat != SpeechResponseFormat.Pcm)
            {
                bytes = await _metaDataService.AddMetaData(bytes, request.ResponseFormat.ToString(), fileName);
            }

            finalStatus = Status.Completed;

            audioFile.Status = finalStatus;
            audioFile.SetDataOnce(bytes);

            await _redisCacheProvider.Set(audioFile.Hash, audioFile.Id);
            await _audioFileRepository.Add(audioFile);
        }
        catch (OperationCanceledException)
        {
            finalStatus = Status.Canceled;
        }
        catch (Exception ex)
        {
            finalStatus = Status.Failed;
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            await UpdateAudioStatus(fileId, finalStatus.ToString(), ownerId, errorMessage: errorMessage, delayMs: StatusUpdateDelayMs);
        }
    }

    public async Task<MemoryStream> CreateSpeechSample(TtsRequestOptions request, string input, string ttsApi,
        string ownerId)
    {
        var hash = AudioFileBuilder.GenerateHash(input, request, AudioType.Sample);

        var bytes = await _redisCacheProvider.GetBytes(hash);

        if (bytes is not null)
        {
            return new MemoryStream(bytes);
        }

        var audioFile = await _audioFileRepository.GetByHash(hash);

        if (audioFile is not null)
        {
            await _redisCacheProvider.SetBytes(hash, audioFile.Data);
            return new MemoryStream(audioFile.Data);
        }

        var bytesCollection = await _ttsServiceFactory.Get(ttsApi)
            .RequestSpeechSample(input, request);

        audioFile = AudioFileBuilder.Create(bytesCollection.ToArray(),
            AudioType.Sample,
            input,
            request,
            ownerId,
            Shared.TtsApis.Single(kv => kv.Key.Equals(ttsApi, StringComparison.OrdinalIgnoreCase)).Value);

        audioFile.Status = Status.Completed;

        await _redisCacheProvider.SetBytes(hash, audioFile.Data);
        await _audioFileRepository.Add(audioFile);

        return new MemoryStream(audioFile.Data);
    }

    private async Task<string> ExtractText(byte[] fileBytes, string fileName)
    {
        var fileProcessor = _fileProcessorFactory.GetProcessor(Path.GetExtension(fileName)) ??
            throw new NotSupportedException("File type not supported");

        var fileText = await fileProcessor.ExtractTextAsync(fileBytes);
        return fileText;
    }

    /// <summary>
    /// Notify clients about the status update
    /// </summary>
    private async Task UpdateAudioStatus(Guid audioFileId,
        string status,
        string ownerId,
        int? progressPercentage = null,
        string? errorMessage = null,
        int? delayMs = null)
    {
        if (delayMs is not null)
        {
            await Task.Delay(delayMs.Value);
        }

        string fileId = audioFileId.ToString();

        try
        {
            _logger.LogInformation("Audio is {Status}. ID: {FileId} ", status, fileId);

            await _hubContext.Clients.Group(ownerId)
                .SendAsync(Shared.AudioStatusUpdated, fileId, status, progressPercentage, errorMessage);

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update status for {FileId}", fileId);
        }
    }

    // todo move to ProgressUpdater class
    private async Task UpdateStatusAndProgress(Guid fileId, ProgressReport report, Status status, string ownerId)
    {
        var shouldTriggerUpdate = !_lastProgressDictionary.TryGetValue(fileId, out var lastProgress)
            || report.ProgressPercentage > lastProgress;

        if (!shouldTriggerUpdate)
        {
            return;
        }

        await UpdateAudioStatus(report.FileId, status.ToString(), ownerId, report.ProgressPercentage).ConfigureAwait(false);

        _lastProgressDictionary[fileId] = report.ProgressPercentage;
    }
}
