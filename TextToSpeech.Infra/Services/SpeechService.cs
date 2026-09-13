using TextToSpeech.Core.Entities;
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
    IRedisCacheProvider _redisCacheProvider) : ISpeechService
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
}
