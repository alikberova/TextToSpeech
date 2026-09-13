using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Audio;
using System.ClientModel;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.Services.Ai;

/// <summary>
/// 50 requests/min for model tts-1
/// </summary>
public sealed class OpenAiService : ITtsService
{
    public int MaxLengthPerApiRequest { get; init; } = 4096;
    // Limit the number of parallel chunk requests to meet rate limits
    private const int MaxParallelChunks = 20;
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAiService> _logger;
    private readonly IProgressTracker _progressTracker;
    private readonly IParallelExecutionService _parallelExecutionService;

    public OpenAiService(
        OpenAIClient client,
        ILogger<OpenAiService> logger,
        IProgressTracker progressTracker,
        IParallelExecutionService parallelExecutionService)
    {
        _client = client;
        _logger = logger;
        _progressTracker = progressTracker;
        _parallelExecutionService = parallelExecutionService;
    }

    private AudioClient AudioClient { get; set; } = default!;

    public async Task<ReadOnlyMemory<byte>[]> RequestSpeechChunksAsync(
        List<string> textChunks,
        Guid fileId,
        TtsRequestOptions ttsRequest,
        IProgress<ProgressReport> progressCallback,
        CancellationToken cancellationToken)
    {
        var totalChunks = textChunks.Count;

        var results = new ReadOnlyMemory<byte>[totalChunks];

        _progressTracker.InitializeFile(fileId, totalChunks);

        await _parallelExecutionService.RunTasksFromItems(
            textChunks,
            MaxParallelChunks,
            async (chunk, index) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytes = await GenerateSpeech(chunk, ttsRequest, cancellationToken);

                results[index] = bytes;

                var progress = _progressTracker.UpdateProgress(fileId, progressCallback, index, 100);

                _logger.LogInformation(
                    "Processed chunk {ChunkIndex}/{TotalChunks} for file {FileId}. Progress: {Progress}%",
                    index + 1,
                    totalChunks,
                    fileId,
                    progress);
            },
            cancellationToken);

        return results;
    }

    public async Task<ReadOnlyMemory<byte>> RequestSpeechSample(
        string text,
        TtsRequestOptions ttsRequest,
        CancellationToken cancellationToken = default)
    {
        return await GenerateSpeech(text, ttsRequest, cancellationToken);
    }

    public Task<List<Voice>?> GetVoices()
    {
        var voices = new List<Voice>
        {
            new() { Name = "Alloy",   ProviderVoiceId = "alloy" },
            new() { Name = "Ash",     ProviderVoiceId = "ash" },
            new() { Name = "Ballad",  ProviderVoiceId = "ballad" },
            new() { Name = "Coral",   ProviderVoiceId = "coral" },
            new() { Name = "Echo",    ProviderVoiceId = "echo" },
            new() { Name = "Fable",   ProviderVoiceId = "fable" },
            new() { Name = "Onyx",    ProviderVoiceId = "onyx" },
            new() { Name = "Nova",    ProviderVoiceId = "nova" },
            new() { Name = "Sage",    ProviderVoiceId = "sage" },
            new() { Name = "Shimmer", ProviderVoiceId = "shimmer" },
            new() { Name = "Verse",   ProviderVoiceId = "verse" }
        };

        return Task.FromResult<List<Voice>?>(voices);
    }

    private async Task<ReadOnlyMemory<byte>> GenerateSpeech(
        string text,
        TtsRequestOptions ttsRequest,
        CancellationToken cancellationToken)
    {
        AudioClient ??= GetClient(ttsRequest.Model!);

        SpeechGenerationOptions options = new() {
            SpeedRatio = Convert.ToSingle(ttsRequest.Speed),
            ResponseFormat = ttsRequest.ResponseFormat.ToString()
        };

        ClientResult<BinaryData> result = await AudioClient.GenerateSpeechAsync(
            text,
            ttsRequest.Voice.ProviderVoiceId,
            options,
            cancellationToken);

        return result.Value.ToMemory();
    }

    private AudioClient GetClient(string model) => _client.GetAudioClient(model);
}
