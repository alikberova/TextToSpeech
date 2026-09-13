using ElevenLabs;
using ElevenLabs.Models;
using ElevenLabs.TextToSpeech;
using ElevenLabs.Voices;
using Microsoft.Extensions.Logging;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Interfaces;
using Voice = TextToSpeech.Core.Models.Voice;

namespace TextToSpeech.Infra.Services.Ai;

public sealed class ElevenLabsService : ITtsService
{
    public int MaxLengthPerApiRequest { get; init; } = 4096;
    private const int MaxParallelChunks = 20;
    private readonly ElevenLabsClient _client;
    private readonly ILogger<ElevenLabsService> _logger;
    private readonly IProgressTracker _progressTracker;
    private readonly IParallelExecutionService _parallelExecutionService;

    public ElevenLabsService(ElevenLabsClient client, ILogger<ElevenLabsService> logger, IProgressTracker progressTracker,
        IParallelExecutionService parallelExecutionService)
    {
        _client = client;
        _logger = logger;
        _progressTracker = progressTracker;
        _parallelExecutionService = parallelExecutionService;
    }

    public async Task<ReadOnlyMemory<byte>[]> RequestSpeechChunksAsync(List<string> textChunks,
        Guid fileId,
        TtsRequestOptions ttsRequest,
        IProgress<ProgressReport> progressCallback,
        CancellationToken cancellationToken)
    {
        var totalChunks = textChunks.Count;
        var results = new ReadOnlyMemory<byte>[totalChunks];

        _progressTracker.InitializeFile(fileId, totalChunks);

        await _parallelExecutionService.RunTasksFromItems(textChunks, MaxParallelChunks,
            async (chunk, index) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytes = await GenerateSpeech(chunk, ttsRequest, cancellationToken);

                results[index] = bytes;

                var progress = _progressTracker.UpdateProgress(fileId, progressCallback, index, 100);

                _logger.LogInformation("Processed chunk {ChunkIndex}/{TotalChunks} for file {FileId}. Progress: {Progress}%",
                    index + 1, totalChunks, fileId, progress);
            },
            cancellationToken);

        return results;
    }

    public async Task<ReadOnlyMemory<byte>> RequestSpeechSample(string text,
        TtsRequestOptions ttsRequest,
        CancellationToken cancellationToken = default)
    {
        return await GenerateSpeech(text, ttsRequest, cancellationToken);
    }

    public async Task<List<Voice>?> GetVoices()
    {
        IReadOnlyList<ElevenLabs.Voices.Voice>? voices = await _client.VoicesEndpoint.GetAllVoicesAsync();

        if (voices is null || voices.Count == 0)
        {
            return null;
        }

        var mapped = voices.Select(v => new Voice
        {
            Name = v.Name.Split(" - ", 2)[0], // remove description
            ProviderVoiceId = v.Id
        }).ToList();

        return mapped;
    }

    private async Task<ReadOnlyMemory<byte>> GenerateSpeech(string text, TtsRequestOptions ttsRequest,
        CancellationToken cancellationToken)
    {
        VoiceSettings voiceSettings = new()
        {
            Speed = Convert.ToSingle(ttsRequest.Speed) // 0.7-1.2
        };

        var elevenLabsVoice = new ElevenLabs.Voices.Voice(ttsRequest.Voice.ProviderVoiceId, ttsRequest.Voice.Name);

        TextToSpeechRequest textToSpeechRequest = new(elevenLabsVoice, text,
            voiceSettings: voiceSettings,
            outputFormat: MapOutputFormat(ttsRequest.ResponseFormat),
            model: new Model(ttsRequest.Model));

        VoiceClip result = await _client.TextToSpeechEndpoint.TextToSpeechAsync(textToSpeechRequest,
            cancellationToken: cancellationToken);

        return result.ClipData;
    }

    private static OutputFormat MapOutputFormat(SpeechResponseFormat format)
    {
        return format.ToString() switch
        {
            "mp3" => OutputFormat.MP3_44100_128,
            "pcm" => OutputFormat.PCM_44100, // pro tier and above
            _ => throw new NotSupportedException($"ElevenLabs does not support the '{format}' format.")
        };
    }
}
