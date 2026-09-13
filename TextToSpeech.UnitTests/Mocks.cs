using Microsoft.Extensions.Logging;
using Moq;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Services;

namespace TextToSpeech.UnitTests;

internal static class Mocks
{
    public static IProgress<ProgressReport> ProgressCallback =>
        new Mock<IProgress<ProgressReport>>().Object;

    public static IProgressTracker ProgressTracker =>
        new Mock<IProgressTracker>().Object;

    public static ProgressTrackerContext CreateProgressContext(Guid fileId)
    {
        var reportedPercentages = new List<int>();
        var progress = new Progress<ProgressReport>(report => reportedPercentages.Add(report.ProgressPercentage));
        var trackerMock = new Mock<IProgressTracker>();

        trackerMock.Setup(t => t.InitializeFile(fileId, It.IsAny<int>()));
        trackerMock.Setup(t => t.UpdateProgress(fileId, It.IsAny<IProgress<ProgressReport>>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<Guid, IProgress<ProgressReport>, int, int>((_, callback, _, chunkProgress) =>
            {
                callback.Report(new ProgressReport { FileId = fileId, ProgressPercentage = chunkProgress });
            })
            .Returns((Guid _, IProgress<ProgressReport> _, int __, int chunkProgress) => chunkProgress);

        return new ProgressTrackerContext(trackerMock, progress, reportedPercentages);
    }

    public static ParallelExecutionService ParallelExecutionService =>
        new(Mock.Of<ILogger<ParallelExecutionService>>());
}

internal sealed record ProgressTrackerContext(Mock<IProgressTracker> TrackerMock, Progress<ProgressReport> Progress, List<int> ReportedPercentages);
