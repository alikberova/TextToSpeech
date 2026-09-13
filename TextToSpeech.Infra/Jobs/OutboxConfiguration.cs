using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Jobs.Transport;
using static TextToSpeech.Infra.Config.ConfigConstants;

namespace TextToSpeech.Infra.Jobs;

public static class OutboxConfiguration
{
    public static IServiceCollection AddOutboxPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqConfig = configuration.GetSection(SectionNames.RabbitMqConfig).Get<RabbitMqConfig>()
            ?? new RabbitMqConfig();

        ValidateRabbitMqConfig(rabbitMqConfig);

        services.Configure<RabbitMqConfig>(configuration.GetSection(SectionNames.RabbitMqConfig));

        services.AddSingleton<IJobDispatchPublisher, RabbitMqJobDispatchPublisher>();
        services.AddScoped<OutboxPublisher>();
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }

    private static void ValidateRabbitMqConfig(RabbitMqConfig config)
    {
        var selectedConnectionName = ConnectionStrings.RabbitMqConnection;
        var selectedConnection = config.GetConnection(selectedConnectionName);
        var missingFields = GetMissingRabbitMqConnectionFields(selectedConnection);

        if (missingFields.Length > 0)
        {
            throw new InvalidOperationException(
                $"RabbitMq configuration is invalid. Missing required fields in {selectedConnectionName}: " +
                $"{string.Join(", ", missingFields)}.");
        }

        if (string.IsNullOrWhiteSpace(config.Exchange) ||
            string.IsNullOrWhiteSpace(config.Queue) ||
            string.IsNullOrWhiteSpace(config.RoutingKey))
        {
            throw new InvalidOperationException("RabbitMq topology names must not be empty.");
        }

        if (config.PublishTimeoutSeconds <= 0 ||
            config.PollIntervalSeconds <= 0 ||
            config.RetryDelaySeconds <= 0)
        {
            throw new InvalidOperationException("RabbitMq timeouts and polling intervals must be positive.");
        }
    }

    private static string[] GetMissingRabbitMqConnectionFields(RabbitMqConnectionConfig connection)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(connection.HostName))
        {
            missing.Add($"{nameof(RabbitMqConnectionConfig.HostName)}");
        }

        if (connection.Port <= 0)
        {
            missing.Add($"{nameof(RabbitMqConnectionConfig.Port)}");
        }

        if (string.IsNullOrWhiteSpace(connection.UserName))
        {
            missing.Add($"{nameof(RabbitMqConnectionConfig.UserName)}");
        }

        if (string.IsNullOrWhiteSpace(connection.Password))
        {
            missing.Add($"{nameof(RabbitMqConnectionConfig.Password)}");
        }

        return [.. missing];
    }
}
