using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.SignalR;

[Authorize]
public sealed class AudioHub(ITaskManager _taskManager) : Hub
{
    public async Task CancelProcessing(Guid audioFileId)
    {
        await _taskManager.TryCancelTask(audioFileId);
    }

    public override async Task OnConnectedAsync()
    {
        var ownerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ownerId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var ownerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ownerId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
