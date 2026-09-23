using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Jobs.Transport;

namespace TextToSpeech.Worker;

public sealed class RabbitMqConsumerService(
    JobRunner runner,
    IOptions<RabbitMqConfig> rabbitOptions,
    IOptions<WorkerConfig> workerOptions,
    ILogger<RabbitMqConsumerService> logger) : BackgroundService
{
    private const string ConnectionName = "worker";

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(Enumerable.Range(0, workerOptions.Value.Concurrency).Select(_ => ConsumeAsync(stoppingToken)));

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeSessionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "RabbitMQ consumer disconnected; unacknowledged deliveries will return");
            }

            await Task.Delay(TimeSpan.FromSeconds(rabbitOptions.Value.RetryDelaySeconds), stoppingToken);
        }
    }

    private async Task ConsumeSessionAsync(CancellationToken stoppingToken)
    {
        using var session = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var config = rabbitOptions.Value;
        var factory = RabbitMqTopology.CreateConnectionFactory(config, ConnectionName);
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            stoppingToken);

        connection.ConnectionShutdownAsync += (_, _) => session.CancelAsync();
        channel.ChannelShutdownAsync += (_, _) => session.CancelAsync();

        await RabbitMqTopology.DeclareAsync(channel, config, stoppingToken);
        await channel.QueueDeclareAsync(RejectedQueue, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.UnregisteredAsync += (_, _) => session.CancelAsync();
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, session);

        await channel.BasicConsumeAsync(config.Queue, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Consuming queue {Queue}", config.Queue);
        await Task.Delay(Timeout.InfiniteTimeSpan, session.Token);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery,
        CancellationTokenSource session)
    {
        try
        {
            var message = Deserialize(delivery.Body);

            if (message is null || !await runner.RunAsync(message, session.Token))
            {
                await QuarantineAsync(channel, delivery, session.Token);
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, session.Token);
        }
        catch (OperationCanceledException) when (session.IsCancellationRequested)
        {
            // Closing the channel requeues the unacknowledged delivery.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not handle delivery {MessageId}", delivery.BasicProperties.MessageId);
            await session.CancelAsync();
        }
    }

    private async Task QuarantineAsync(IChannel channel, BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = delivery.BasicProperties.MessageId,
            CorrelationId = delivery.BasicProperties.CorrelationId,
            ContentType = delivery.BasicProperties.ContentType
        };

        timeout.CancelAfter(TimeSpan.FromSeconds(rabbitOptions.Value.PublishTimeoutSeconds));
        // Confirm quarantine before acknowledging; the existing dispatch queue needs no incompatible DLX change.
        await channel.BasicPublishAsync(string.Empty, RejectedQueue, mandatory: true,
            properties, delivery.Body, timeout.Token);
        logger.LogWarning("Quarantined unsupported or malformed message {MessageId}", properties.MessageId);
    }

    private string RejectedQueue => rabbitOptions.Value.Queue + ".rejected";

    private static JobDispatchMessage? Deserialize(ReadOnlyMemory<byte> body)
    {
        try
        {
            var message = JsonSerializer.Deserialize<JobDispatchMessage>(body.Span);

            return message?.JobId == Guid.Empty ? null : message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

