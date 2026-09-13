using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Infra.Storage;

public sealed class FileSystemArtifactStorage(IPathService pathService) : IArtifactStorage
{
    private const string ObjectsDirectory = "objects";
    private const string PendingExtension = ".pending";

    public async Task<Guid> WriteAsync(byte[] content, CancellationToken cancellationToken)
    {
        var objectId = Guid.NewGuid();
        var finalPath = GetPath(objectId);
        var pendingPath = finalPath + PendingExtension;

        try
        {
            await using (var stream = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true))
            {
                await stream.WriteAsync(content, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(pendingPath, finalPath);

            return objectId;
        }
        finally
        {
            // A failed write must never expose a partially written object under its final ID.
            if (File.Exists(pendingPath))
            {
                File.Delete(pendingPath);
            }
        }
    }

    public Task<Stream> OpenReadAsync(Guid objectId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stream stream = new FileStream(GetPath(objectId), FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);

        return Task.FromResult(stream);
    }

    private string GetPath(Guid objectId)
    {
        var directory = Path.Combine(pathService.GetFileStoragePath(), ObjectsDirectory);

        Directory.CreateDirectory(directory);

        return Path.Combine(directory, objectId.ToString("N"));
    }
}
