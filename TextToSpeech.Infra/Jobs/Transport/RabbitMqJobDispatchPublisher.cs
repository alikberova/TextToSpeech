using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Config;

namespace TextToSpeech.Infra.Jobs.Transport;

public sealed class RabbitMqJobDispatchPublisher(IOptions<RabbitMqConfig> options)
    : IJobDispatchPublisher, IAsyncDisposable
{
    private const string ContentType = "application/json";
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(Guid messageId, JobDispatchMessage message, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.PublishTimeoutSeconds));
        await _channelLock.WaitAsync(timeout.Token);

        try
        {
            var channel = await GetChannelAsync(timeout.Token);
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = ContentType,
                MessageId = messageId.ToString(),
                CorrelationId = message.CorrelationId.ToString(),
                Type = nameof(JobDispatchMessage)
            };

            await channel.BasicPublishAsync(options.Value.Exchange, options.Value.RoutingKey, mandatory: true,
                properties, JsonSerializer.SerializeToUtf8Bytes(message), timeout.Token);
        }
        catch
        {
            // Discard an uncertain channel; the durable outbox will retry with the same message ID.
            await CloseConnectionAsync();
            throw;
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
        {
            return _channel;
        }

        await CloseConnectionAsync();

        var connection = options.Value.RabbitMqConnection;
        var factory = new ConnectionFactory
        {
            HostName = connection.HostName,
            Port = connection.Port,
            UserName = connection.UserName,
            Password = connection.Password,
            VirtualHost = connection.VirtualHost,
            AutomaticRecoveryEnabled = false
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        await _channel.ExchangeDeclareAsync(options.Value.Exchange, ExchangeType.Direct, durable: true,
            autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(options.Value.Queue, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(options.Value.Queue, options.Value.Exchange, options.Value.RoutingKey,
            cancellationToken: cancellationToken);

        return _channel;
    }

    private async Task CloseConnectionAsync()
    {
        var channel = _channel;
        var connection = _connection;
        _channel = null;
        _connection = null;

        try
        {
            if (channel is not null)
            {
                await channel.DisposeAsync();
            }
        }
        finally
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseConnectionAsync();
        _channelLock.Dispose();
    }
}
