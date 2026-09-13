using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.Services;

public sealed class VoiceService : IVoiceService
{
    private readonly ITtsServiceFactory _ttsServiceFactory;
    private readonly IRedisCacheProvider _redisCacheProvider;

    public VoiceService(ITtsServiceFactory ttsServiceFactory, IRedisCacheProvider redisCacheProvider)
    {
        _ttsServiceFactory = ttsServiceFactory;
        _redisCacheProvider = redisCacheProvider;
    }

    public async Task<List<Voice>?> GetVoices(string provider)
    {
        var cacheKey = CacheKeys.Voices(provider);

        var cachedVoices = await _redisCacheProvider.Get<List<Voice>>(cacheKey);

        if (cachedVoices is not null)
        {
            return cachedVoices;
        }

        var ttsService = _ttsServiceFactory.Get(provider);

        var voices = await ttsService.GetVoices();

        if (voices is not null)
        {
            await _redisCacheProvider.Set(cacheKey, voices);
        }

        return voices;
    }
}
