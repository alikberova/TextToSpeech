using MailKit.Net.Smtp;
using TextToSpeech.Api.Services;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Infra;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Jobs;
using TextToSpeech.Infra.Repositories;
using TextToSpeech.Infra.Services;
using TextToSpeech.Infra.Services.Common;
using TextToSpeech.Infra.Services.FileProcessing;
using TextToSpeech.Infra.SignalR;
using TextToSpeech.Infra.Storage;

namespace TextToSpeech.Api.Extensions;

internal static class ServicesDiExtension
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDbInitializer, DbInitializer>();
        services.AddBackgroundJobs();
        services.AddOutboxPublisher(configuration);

        services.AddScoped<IAudioFileRepository, AudioFileRepository>();

        services.AddScoped<ISpeechService, SpeechService>();
        services.AddScoped<ISubmitSpeechGeneration, SubmitSpeechGeneration>();
        services.AddScoped<ISpeechGenerationRequests, SpeechGenerationRequests>();
        services.AddSingleton<IArtifactStorage, FileSystemArtifactStorage>();
        services.AddScoped<ISpeechGenerationNotifications, SpeechGenerationNotifications>();

        services.AddScoped<IMetaDataService, MetaDataService>();
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

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOwnerContext, HttpOwnerContext>();

        services.AddScoped<IEmailService, EmailService>();
        services.AddTtsProviders(configuration);
        services.AddSingleton<SpeechJobConnections>();
        services.AddHostedService<SpeechJobStatusRelay>();

        services.AddRedis(configuration);

        return services;
    }
}
