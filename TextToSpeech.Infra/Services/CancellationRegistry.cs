using System.Collections.Concurrent;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.Services;

public class CancellationRegistry : ICancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, (string OwnerId, CancellationTokenSource Source)> _processingTasks = new();
    private readonly SemaphoreSlim _cancellationLock = new(1, 1);

    public void AddTask(Guid fileId, string ownerId, CancellationTokenSource cts)
    {
        _processingTasks[fileId] = (ownerId, cts);
    }

    public async Task<bool> TryCancelTask(Guid fileId, string ownerId)
    {
        await _cancellationLock.WaitAsync();
        try
        {
            if (_processingTasks.TryGetValue(fileId, out var registration))
            {
                var (taskOwnerId, source) = registration;
                if (taskOwnerId == ownerId)
                {
                    await source.CancelAsync();
                    return true;
                }
            }
            return false;
        }
        finally
        {
            _cancellationLock.Release();
        }
    }

    public async Task CompleteTaskAsync(Guid fileId)
    {
        await _cancellationLock.WaitAsync();
        try
        {
            if (_processingTasks.TryRemove(fileId, out var registration))
            {
                var (_, source) = registration;
                source.Dispose();
            }
        }
        finally
        {
            _cancellationLock.Release();
        }
    }
}
