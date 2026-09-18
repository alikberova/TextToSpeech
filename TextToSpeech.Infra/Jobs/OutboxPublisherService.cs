using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TextToSpeech.Infra.Config;

namespace TextToSpeech.Infra.Jobs;

public sealed class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqConfig> options,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();

                if (await publisher.PublishNextAsync(stoppingToken))
                {
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publishing failed; unpublished messages will be retried");
                delay = TimeSpan.FromSeconds(options.Value.RetryDelaySeconds);
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
