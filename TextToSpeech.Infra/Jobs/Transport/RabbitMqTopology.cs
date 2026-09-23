using RabbitMQ.Client;
using TextToSpeech.Infra.Config;

namespace TextToSpeech.Infra.Jobs.Transport;

public static class RabbitMqTopology
{
    public static ConnectionFactory CreateConnectionFactory(RabbitMqConfig config, string clientProvidedName)
    {
        var connection = config.RabbitMqConnection;

        return new ConnectionFactory
        {
            HostName = connection.HostName,
            Port = connection.Port,
            UserName = connection.UserName,
            Password = connection.Password,
            VirtualHost = connection.VirtualHost,
            ClientProvidedName = clientProvidedName,
            AutomaticRecoveryEnabled = false
        };
    }

    public static async Task DeclareAsync(IChannel channel, RabbitMqConfig config, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(config.Exchange, ExchangeType.Direct, durable: true,
            autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(config.Queue, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(config.Queue, config.Exchange, config.RoutingKey,
            cancellationToken: cancellationToken);
    }
}

