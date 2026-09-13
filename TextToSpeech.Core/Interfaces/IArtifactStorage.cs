namespace TextToSpeech.Core.Interfaces;

public interface IArtifactStorage
{
    // Each write creates an immutable object. Callers persist its logical ID, never a physical path.
    Task<Guid> WriteAsync(byte[] content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(Guid objectId, CancellationToken cancellationToken);
}
