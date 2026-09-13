using System.Collections.Concurrent;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.Services;

public class TaskManager : ITaskManager
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _processingTasks = new();

    public void AddTask(Guid fileId, CancellationTokenSource cts)
    {
        _processingTasks[fileId] = cts;
    }

    public async Task<bool> TryCancelTask(Guid fileId)
    {
        if (_processingTasks.TryRemove(fileId, out var cts))
        {
            await cts.CancelAsync();
            return true;
        }
        return false;
    }
}
