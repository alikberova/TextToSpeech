namespace TextToSpeech.Infra.Config;

public sealed class RabbitMqConfig
{
    public RabbitMqConnectionConfig RabbitMqConnection { get; init; } = new();
    public RabbitMqConnectionConfig RabbitMqTestConnection { get; init; } = new();
    public string Exchange { get; init; } = string.Empty;
    public string Queue { get; init; } = string.Empty;
    public string RoutingKey { get; init; } = string.Empty;
    public int PublishTimeoutSeconds { get; init; }
    public int PollIntervalSeconds { get; init; }
    public int RetryDelaySeconds { get; init; }

    public RabbitMqConnectionConfig GetConnection(string connectionName) =>
        connectionName == ConfigConstants.ConnectionStrings.RabbitMqTestConnection
            ? RabbitMqTestConnection
            : RabbitMqConnection;
}

public sealed class RabbitMqConnectionConfig
{
    public string HostName { get; init; } = string.Empty;
    public int Port { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = "/";
}
