using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Config;
using TextToSpeech.Infra.Jobs;
using TextToSpeech.Infra.Jobs.Transport;

namespace TextToSpeech.IntegrationTests.Tests;

public sealed class OutboxPublisherTests(PostgreSqlFixture database, RabbitMqFixture broker)
    : IClassFixture<PostgreSqlFixture>, IClassFixture<RabbitMqFixture>
{
    [Fact]
    public async Task Publish_UncertainOutcome_RetriesWithTheSameMessageId()
    {
        var options = CreateOptions();
        var accepted = await SubmitAsync();
        await using var transport = new RabbitMqJobDispatchPublisher(Options.Create(options));
        var uncertain = new Mock<IJobDispatchPublisher>();

        uncertain.Setup(p => p.PublishAsync(It.IsAny<Guid>(), It.IsAny<JobDispatchMessage>(), CancellationToken.None))
            .Returns(async (Guid id, JobDispatchMessage message, CancellationToken token) =>
            {
                await transport.PublishAsync(id, message, token);
                throw new IOException(); // Broker accepted; caller did not observe success.
            });

        await Assert.ThrowsAsync<IOException>(() => PublishAsync(uncertain.Object));
        Assert.True(await PublishAsync(transport));
        Assert.False(await PublishAsync(transport));

        await using var connection = await OpenConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var first = await channel.BasicGetAsync(options.Queue, autoAck: true);
        var retry = await channel.BasicGetAsync(options.Queue, autoAck: true);

        Assert.NotNull(first);
        Assert.NotNull(retry);
        Assert.Equal(first.BasicProperties.MessageId, retry.BasicProperties.MessageId);
        Assert.True(first.BasicProperties.Persistent);
        Assert.Equal(first.Body.ToArray(), retry.Body.ToArray());

        var dispatch = JsonSerializer.Deserialize<JobDispatchMessage>(retry.Body.Span);

        Assert.NotNull(dispatch);
        Assert.Equal(accepted.JobId, dispatch.JobId);
        Assert.Equal(JobDispatchMessage.CurrentVersion, dispatch.ContractVersion);
    }

    [Fact]
    public async Task Publish_UnroutableMessage_RemainsAvailableForRetry()
    {
        var options = CreateOptions();
        await using var transport = new RabbitMqJobDispatchPublisher(Options.Create(options));
        var warmup = new JobDispatchMessage(Guid.NewGuid(), JobDispatchMessage.CurrentVersion, Guid.NewGuid());

        await transport.PublishAsync(Guid.NewGuid(), warmup, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.BasicGetAsync(options.Queue, autoAck: true);
        await channel.QueueUnbindAsync(options.Queue, options.Exchange, options.RoutingKey);

        var accepted = await SubmitAsync();

        await Assert.ThrowsAsync<PublishReturnException>(() => PublishAsync(transport));
        Assert.True(await PublishAsync(transport));
        Assert.False(await PublishAsync(transport));

        var received = await channel.BasicGetAsync(options.Queue, autoAck: true);

        Assert.NotNull(received);
        Assert.Equal(accepted.JobId, JsonSerializer.Deserialize<JobDispatchMessage>(received.Body.Span)!.JobId);
    }

    private RabbitMqConfig CreateOptions() => new()
    {
        RabbitMqConnection = new RabbitMqConnectionConfig
        {
            HostName = broker.HostName,
            Port = broker.Port,
            UserName = broker.UserName,
            Password = broker.Password,
            VirtualHost = broker.VirtualHost
        },
        RabbitMqTestConnection = new RabbitMqConnectionConfig
        {
            HostName = broker.HostName,
            Port = broker.Port,
            UserName = broker.UserName,
            Password = broker.Password,
            VirtualHost = broker.VirtualHost
        },
        Exchange = Guid.NewGuid().ToString(),
        Queue = Guid.NewGuid().ToString(),
        RoutingKey = "dispatch",
        PublishTimeoutSeconds = 10,
        PollIntervalSeconds = 1,
        RetryDelaySeconds = 5
    };

    private Task<IConnection> OpenConnectionAsync() =>
        new ConnectionFactory { Uri = new Uri(broker.ConnectionString) }.CreateConnectionAsync();

    private async Task<bool> PublishAsync(IJobDispatchPublisher transport)
    {
        await using var context = database.CreateContext();

        return await new OutboxPublisher(context, transport).PublishNextAsync(CancellationToken.None);
    }

    private async Task<JobAcceptance> SubmitAsync()
    {
        await using var context = database.CreateContext();

        await context.Database.MigrateAsync();

        var submission = new JobSubmission("test-job", 1, Guid.NewGuid(), Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid());

        return await new SubmitBackgroundJob(context).SubmitAsync(submission, CancellationToken.None);
    }
}
