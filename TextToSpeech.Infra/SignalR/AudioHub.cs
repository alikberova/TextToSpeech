using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Jobs;

namespace TextToSpeech.Infra.SignalR;

[Authorize]
public sealed class AudioHub(
    ICancellationRegistry cancellationRegistry,
    ISpeechGenerationRequests requests,
    IBackgroundJobs jobs) : Hub
{
    public async Task CancelProcessing(Guid audioFileId)
    {
        var ownerId = GetOwnerId();
        if (ownerId is not null)
        {
            var jobId = await requests.GetJobIdAsync(audioFileId, ownerId, Context.ConnectionAborted);

            if (jobId.HasValue && await jobs.RequestCancellationAsync(jobId.Value, ownerId, Context.ConnectionAborted))
            {
                await cancellationRegistry.TryCancelTask(audioFileId, ownerId);
            }
        }
    }

    public override async Task OnConnectedAsync()
    {
        var ownerId = GetOwnerId();

        if (ownerId is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ownerId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var ownerId = GetOwnerId();

        if (ownerId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ownerId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private string? GetOwnerId()
    {
        var ownerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(ownerId) ? null : ownerId;
    }
}
