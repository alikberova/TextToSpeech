using System.Collections.Concurrent;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Services;

public sealed class ProgressTracker : IProgressTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, int>> progressDictionary = new();

    public int UpdateProgress(Guid fileId, IProgress<ProgressReport> progressCallback, int chunkIndex, int chunkProgress)
    {
        var indexAndProgressDict = progressDictionary.GetOrAdd(fileId, _ => new ConcurrentDictionary<int, int>());
        indexAndProgressDict[chunkIndex] = chunkProgress;

        // overall = (sum of all the chunk percents) / chunkCount
        // example for 3 chunks: overall = (20 + 50 + 90) / 3 = 53%

        int allChunksPercent = indexAndProgressDict.Values.Sum();
        int chunksCount = indexAndProgressDict.Count;

        var overallPercentage = allChunksPercent / chunksCount;

        progressCallback.Report(new ProgressReport()
        {
            FileId = fileId,
            ProgressPercentage = overallPercentage
        });

        return overallPercentage;
    }

    public void InitializeFile(Guid fileId, int totalChunks)
    {
        var dict = progressDictionary.GetOrAdd(fileId, _ => new ConcurrentDictionary<int, int>());

        for (int i = 0; i < totalChunks; i++)
        {
            dict.TryAdd(i, 0);
        }
    }
}
