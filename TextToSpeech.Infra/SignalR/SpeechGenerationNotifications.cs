using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Infra.Constants;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.SignalR;

public sealed class SpeechGenerationNotifications(
    IHubContext<AudioHub> hubContext,
    ILogger<SpeechGenerationNotifications> logger) : ISpeechGenerationNotifications
{
    private const int StatusUpdateDelayMs = 200; // todo not stable

    /// <summary>
    /// Notify clients about the status update.
    /// </summary>
    public async Task PublishAsync(Guid fileId, string ownerId, Status status,
        int? progressPercentage = null, string? errorMessage = null)
    {
        try
        {
            if (status is Status.Created or Status.Completed or Status.Failed or Status.Canceled)
            {
                await Task.Delay(StatusUpdateDelayMs);
            }

            await hubContext.Clients.Group(ownerId).SendAsync(Shared.AudioStatusUpdated,
                fileId.ToString(), status.ToString(), progressPercentage, errorMessage);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update status for {fileId}", fileId);
        }
    }
}
