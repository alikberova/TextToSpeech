using Microsoft.Extensions.DependencyInjection;
using TextToSpeech.Core.Interfaces.Ai;

namespace TextToSpeech.Infra.Services.Ai;

public sealed class TtsServiceFactory(IServiceProvider serviceProvider) : ITtsServiceFactory
{
    public ITtsService Get(string key)
    {
        return serviceProvider.GetRequiredKeyedService<ITtsService>(key);
    }
}