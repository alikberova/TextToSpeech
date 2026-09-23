using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Core.Services;
using TextToSpeech.Infra;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Jobs;
using TextToSpeech.Infra.Repositories;
using TextToSpeech.Infra.Services;
using TextToSpeech.Infra.Services.Common;
using TextToSpeech.Infra.Storage;

namespace TextToSpeech.Worker;

public static class WorkerConfiguration
{
    public static IServiceCollection AddJobWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WorkerConfig>()
            .Bind(configuration.GetSection(WorkerConfig.SectionName))
            .Validate(x => x.Concurrency > 0 && x.HeartbeatSeconds > 0 &&
                x.LeaseSeconds > x.HeartbeatSeconds && x.RecoveryIntervalSeconds > 0,
                "Worker concurrency and intervals must be positive; heartbeat must be shorter than lease.")
            .ValidateOnStart();
        services.AddRabbitMqConfiguration(configuration);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(GetRequiredConnectionString(
                configuration,
                ConfigConstants.ConnectionStrings.DbConnection)));
        services.AddBackgroundJobs();
        services.AddScoped<WorkerJobReader>();
        services.AddSingleton<JobRunner>();
        services.AddSingleton<JobHeartbeat>();
        services.AddHostedService<RabbitMqConsumerService>();
        services.AddHostedService<JobRecoveryService>();

        return services;
    }

    public static IServiceCollection AddSpeechGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        ValidateRequiredValue(configuration[ConfigConstants.AppDataPath], ConfigConstants.AppDataPath);
        services.AddTtsProviders(configuration);
        services.AddScoped<ISpeechGenerationRequests, SpeechGenerationRequests>();
        services.AddScoped<IAudioFileRepository, AudioFileRepository>();
        services.AddScoped<IExecuteSpeechGeneration, ExecuteSpeechGeneration>();
        services.AddKeyedScoped<IBackgroundJobHandler, SpeechGenerationJobHandler>(SpeechGenerationRequests.JobType);
        services.AddScoped<IMetaDataService, MetaDataService>();
        services.AddScoped<IParallelExecutionService, ParallelExecutionService>();
        services.AddSingleton<ITextProcessingService, TextProcessingService>();
        services.AddSingleton<IPathService, PathService>();
        services.AddSingleton<IArtifactStorage, FileSystemArtifactStorage>();

        return services;
    }

    private static void ValidateRequiredValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{name}' is required.");
        }
    }

    private static string GetRequiredConnectionString(IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' is required.");
        }

        return connectionString;
    }
}
