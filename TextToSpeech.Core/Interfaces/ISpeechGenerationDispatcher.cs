namespace TextToSpeech.Core.Interfaces;

public interface ISpeechGenerationDispatcher
{
    Task DispatchAsync(Guid jobId, Guid fileId, string ownerId, CancellationToken cancellationToken);
}
