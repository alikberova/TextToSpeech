using ElevenLabs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Services;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Services.Ai;
using TextToSpeech.Infra.Stubs;

namespace TextToSpeech.Infra.Config;

public static class TtsServicesConfiguration
{
    public static IServiceCollection AddTtsProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITtsServiceFactory, TtsServiceFactory>();
        services.AddScoped<IProgressTracker, ProgressTracker>();
        services.Configure<NarakeetConfig>(configuration.GetSection(ConfigConstants.SectionNames.NarakeetConfig));

        ConfigurationChecks.Validate(configuration);
        RegisterServicesBasedOnTestMode(services, configuration);

        return services;
    }

    private static void RegisterServicesBasedOnTestMode(IServiceCollection services, IConfiguration configuration)
    {
        var isTestMode = HostingEnvironment.IsTestMode();

        services.AddKeyedScoped<ITtsService, ElevenLabsService>(Shared.ElevenLabs.Key);
        services.AddSingleton(_ => isTestMode
            ? FakeElevenLabsClient.Create()
            : new ElevenLabsClient(configuration[ConfigConstants.ElevenLabsApiKey]));

        services.AddKeyedScoped<ITtsService, OpenAiService>(Shared.OpenAI.Key);
        services.AddSingleton(_ => isTestMode
            ? FakeOpenAIClient.Create()
            : new OpenAIClient(configuration[ConfigConstants.OpenAiApiKey]));

        services.AddKeyedScoped<ITtsService>(Shared.Narakeet.Key,
            (provider, _) => provider.GetRequiredService<NarakeetService>());
        services.AddHttpClient<NarakeetService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<NarakeetConfig>>().Value;

            client.BaseAddress = new Uri(options.ApiUrl);
            client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
        })
            .ConfigurePrimaryHttpMessageHandler(() =>
                isTestMode ? new FakeNarakeetHandler() : new HttpClientHandler());
    }
}
