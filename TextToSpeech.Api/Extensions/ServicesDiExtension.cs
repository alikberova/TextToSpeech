using ElevenLabs;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using OpenAI;
using TextToSpeech.Api.Services;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Services;
using TextToSpeech.Infra;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Repositories;
using TextToSpeech.Infra.Services;
using TextToSpeech.Infra.Services.Ai;
using TextToSpeech.Infra.Services.Common;
using TextToSpeech.Infra.Services.FileProcessing;
using TextToSpeech.Infra.Stubs;

namespace TextToSpeech.Api.Extensions;

internal static class ServicesDiExtension
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDbInitializer, DbInitializer>();

        services.AddScoped<IAudioFileRepository, AudioFileRepository>();
        services.AddScoped<ISpeechService, SpeechService>();
        services.AddScoped<IMetaDataService, MetaDataService>();
        services.AddScoped<ITtsServiceFactory, TtsServiceFactory>();
        services.AddScoped<ISmtpClient, SmtpClient>();
        services.AddScoped<IVoiceService, VoiceService>();
        services.AddScoped<IParallelExecutionService, ParallelExecutionService>();
        services.AddScoped<ITestSeedService, TestSeedService>();

        services.AddSingleton<ITextProcessingService, TextProcessingService>();
        services.AddSingleton<IPathService, PathService>();
        services.AddSingleton<IFileProcessorFactory, FileProcessorFactory>();
        services.AddSingleton<IFileProcessor, TextFileProcessor>();
        services.AddSingleton<IFileProcessor, PdfProcessor>();
        services.AddSingleton<IFileProcessor, EpubProcessor>();
        services.AddSingleton<ITaskManager, TaskManager>();
        services.AddSingleton<IProgressTracker, ProgressTracker>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        services.AddHostedService<QueuedHostedService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOwnerContext, HttpOwnerContext>();

        RegisterServicesBasedOnTestMode(services, configuration);

        services.AddRedis(configuration);

        return services;
    }

    private static void RegisterServicesBasedOnTestMode(IServiceCollection services, IConfiguration configuration)
    {
        var isTestMode = HostingEnvironment.IsTestMode();

        services.AddScoped<IEmailService, EmailService>();

        services.AddKeyedScoped<ITtsService, ElevenLabsService>(Shared.ElevenLabs.Key);
        services.AddSingleton(serviceProvider =>
        {
            return isTestMode
                ? FakeElevenLabsClient.Create()
                : new ElevenLabsClient(configuration[ConfigConstants.ElevenLabsApiKey]);
        });

        services.AddKeyedScoped<ITtsService, OpenAiService>(Shared.OpenAI.Key);
        services.AddSingleton(serviceProvider =>
        {
            return isTestMode
                ? FakeOpenAIClient.Create()
                : new OpenAIClient(configuration[ConfigConstants.OpenAiApiKey]);
        });

        services.AddKeyedScoped<ITtsService>(Shared.Narakeet.Key,
            (sp, _) => sp.GetRequiredService<NarakeetService>());
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
