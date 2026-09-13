using TextToSpeech.Core.Entities;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Services.FileProcessing;
using static TextToSpeech.Infra.TestData;

namespace TextToSpeech.Infra.Services;

public sealed class TestSeedService : ITestSeedService
{
    private readonly IAudioFileRepository _audioRepository;
    private readonly IRedisCacheProvider _cacheProvider;

    public TestSeedService(
        IAudioFileRepository audioRepository,
        IRedisCacheProvider cacheProvider)
    {
        _audioRepository = audioRepository;
        _cacheProvider = cacheProvider;
    }

    public async Task SeedAsync()
    {
        await SeedAudioFilesAsync();
        await SeedVoicesCacheAsync();
    }

    private async Task SeedAudioFilesAsync()
    {
        var audios = new List<(AudioFile Audio, byte[] Content)>
        {
            (CreateAudioSampleAlloy(), AudioFileService.GenerateSilentMp3(5)),
            (CreateAudioFullFable(), AudioFileService.GenerateSilentMp3(3))
        };

        foreach (var (audio, content) in audios)
        {
            var existing = await _audioRepository.GetByIdAsNoTracking(audio.Id);

            if (existing is null)
            {
                await _audioRepository.Add(audio, content);
                continue;
            }

            if (existing.Hash != audio.Hash)
            {
                await _audioRepository.Update(audio, content);
            }
        }
    }

    private async Task SeedVoicesCacheAsync()
    {
        await _cacheProvider.Set(
            CacheKeys.Voices(Shared.OpenAI.Key),
            OpenAiVoices.All,
            TimeSpan.FromDays(1));

        await _cacheProvider.Set(
            CacheKeys.Voices(Shared.Narakeet.Key),
            NarakeetVoices.All,
            TimeSpan.FromDays(1));
    }
}

