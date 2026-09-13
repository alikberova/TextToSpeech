namespace TextToSpeech.Infra.Interfaces;

public interface ICancellationRegistry
{
    void AddTask(Guid fileId, string ownerId, CancellationTokenSource cts);
    Task<bool> TryCancelTask(Guid fileId, string ownerId);
    Task CompleteTaskAsync(Guid fileId);
}
