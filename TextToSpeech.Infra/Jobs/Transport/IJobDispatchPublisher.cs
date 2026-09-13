using TextToSpeech.Core.Jobs;

namespace TextToSpeech.Infra.Jobs.Transport;

public interface IJobDispatchPublisher
{
    // Returns only after the broker confirms a routed, persistent message.
    Task PublishAsync(Guid messageId, JobDispatchMessage message, CancellationToken cancellationToken);
}
