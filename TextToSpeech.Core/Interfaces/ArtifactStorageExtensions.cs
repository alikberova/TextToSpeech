namespace TextToSpeech.Core.Interfaces;

public static class ArtifactStorageExtensions
{
    public static async Task<byte[]> ReadBytesAsync(
        this IArtifactStorage storage,
        Guid objectId,
        CancellationToken cancellationToken)
    {
        await using var source = await storage.OpenReadAsync(objectId, cancellationToken);
        using var destination = new MemoryStream();

        await source.CopyToAsync(destination, cancellationToken);

        return destination.ToArray();
    }
}
