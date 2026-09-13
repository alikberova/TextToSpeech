using TextToSpeech.Core.Entities;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
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
        var audios = new List<AudioFile>
        {
            CreateAudioSampleAlloy(),
            CreateAudioFullFable()
        };

        foreach (var audio in audios)
        {
            var existing = await _audioRepository.GetByIdAsNoTracking(audio.Id);

            if (existing is null)
            {
                await _audioRepository.Add(audio);
                continue;
            }

            if (existing.Hash != audio.Hash)
            {
                await _audioRepository.Update(audio);
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

