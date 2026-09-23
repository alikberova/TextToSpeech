using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Core.Interfaces;

public interface ISpeechGenerationNotifications
{
    void Refresh(string ownerId);

    Task PublishAsync(Guid fileId, string ownerId, Status status,
        int? progressPercentage = null, string? errorMessage = null);
}
