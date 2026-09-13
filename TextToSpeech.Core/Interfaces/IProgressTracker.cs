using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Interfaces;

public interface IProgressTracker
{
    int UpdateProgress(Guid fileId, IProgress<ProgressReport> progressCallback, int chunkIndex, int chunkProgress);

    void InitializeFile(Guid fileId, int totalChunks);
}