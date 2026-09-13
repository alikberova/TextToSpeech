using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Core.Interfaces;

public interface ISpeechGenerationNotifications
{
    Task PublishAsync(Guid fileId, string ownerId, Status status,
        int? progressPercentage = null, string? errorMessage = null);
}
