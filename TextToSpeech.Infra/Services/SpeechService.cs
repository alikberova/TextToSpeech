using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

public sealed class SpeechService(ITtsServiceFactory _ttsServiceFactory,
    IAudioFileRepository _audioFileRepository,
    IRedisCacheProvider _redisCacheProvider,
    IArtifactStorage storage) : ISpeechService
{
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
            bytes = await storage.ReadBytesAsync(audioFile.ContentId, CancellationToken.None);

            await _redisCacheProvider.SetBytes(hash, bytes);

            return new MemoryStream(bytes);
        }

        var bytesCollection = await _ttsServiceFactory.Get(ttsApi)
            .RequestSpeechSample(input, request);

        bytes = bytesCollection.ToArray();
        audioFile = AudioFileBuilder.Create(AudioType.Sample,
            input,
            request,
            ownerId,
            Shared.TtsApis.Single(kv => kv.Key.Equals(ttsApi, StringComparison.OrdinalIgnoreCase)).Value);

        audioFile.Status = Status.Completed;

        await _redisCacheProvider.SetBytes(hash, bytes);
        await _audioFileRepository.Add(audioFile, bytes);

        return new MemoryStream(bytes);
    }
}
