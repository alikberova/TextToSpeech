using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Services;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.SignalR;

public sealed class SpeechJobStatusRelay(
    SpeechJobConnections connections,
    IServiceScopeFactory scopeFactory,
    IHubContext<AudioHub> hub,
    ILogger<SpeechJobStatusRelay> logger) : BackgroundService
{
    private readonly Dictionary<string, Dictionary<Guid, JobSnapshot>> _sent = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PublishChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not relay persisted speech job statuses");
            }
        }
    }

    private async Task PublishChangesAsync(CancellationToken cancellationToken)
    {
        var active = connections.Snapshot();
        var disconnected = _sent.Keys.Except(active.Select(x => x.Key)).ToArray();
        var refresh = active.Where(x => connections.TakeRefresh(x.Key) || !_sent.ContainsKey(x.Key)).ToArray();

        foreach (var connectionId in disconnected.Concat(refresh.Select(x => x.Key)))
        {
            _sent.Remove(connectionId);
        }

        if (refresh.Length > 0)
        {
            await Task.Delay(SpeechGenerationNotifications.StatusUpdateDelayMs, cancellationToken);
        }

        foreach (var owner in active.GroupBy(x => x.Value))
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var jobs = await context.BackgroundJobs
                .AsNoTracking()
                .Where(x => x.OwnerId == owner.Key && x.JobType == SpeechGenerationRequests.JobType)
                .ToListAsync(cancellationToken);

            foreach (var connection in owner)
            {
                foreach (var job in jobs)
                {
                    await PublishAsync(connection.Key, job.Snapshot(), cancellationToken);
                }
            }
        }
    }

    private async Task PublishAsync(string connectionId, JobSnapshot job, CancellationToken cancellationToken)
    {
        if (!_sent.TryGetValue(connectionId, out var previous))
        {
            previous = [];
            _sent.Add(connectionId, previous);
        }

        if (previous.TryGetValue(job.Id, out var last) && last == job)
        {
            return;
        }

        var status = job.Status switch
        {
            JobStatus.Completed => Status.Completed,
            JobStatus.Cancelled => Status.Canceled,
            JobStatus.Failed or JobStatus.RecoveryRequired => Status.Failed,
            JobStatus.Running => Status.Processing,
            _ => Status.Created
        };

        // A new connection receives current durable state, including completion missed while disconnected.
        await hub.Clients.Client(connectionId).SendAsync(Shared.AudioStatusUpdated, job.InputId.ToString(),
            status.ToString(), job.Progress, job.ErrorCode, cancellationToken);
        previous[job.Id] = job;
    }
}
