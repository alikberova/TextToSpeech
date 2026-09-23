using Microsoft.Extensions.DependencyInjection;
using Moq;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Models;
using TextToSpeech.Core.Services;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class ProgressTrackerTests
{
    [Fact]
    public void UpdateProgress_AveragesAcrossAllChunksAndReports()
    {
        var tracker = new ProgressTracker();
        var fileId = Guid.NewGuid();
        var reportedPercentages = new List<int>();
        var progress = new Mock<IProgress<ProgressReport>>();
        progress.Setup(p => p.Report(It.IsAny<ProgressReport>()))
            .Callback<ProgressReport>(report =>
            {
                Assert.Equal(fileId, report.FileId);
                reportedPercentages.Add(report.ProgressPercentage);
            });

        tracker.InitializeFile(fileId, 3);

        var first = tracker.UpdateProgress(fileId, progress.Object, 0, 100);
        var second = tracker.UpdateProgress(fileId, progress.Object, 1, 100);
        var third = tracker.UpdateProgress(fileId, progress.Object, 2, 100);

        Assert.Equal(33, first);
        Assert.Equal(66, second);
        Assert.Equal(100, third);
        Assert.Equal(new[] { 33, 66, 100 }, reportedPercentages);
        progress.Verify(p => p.Report(It.IsAny<ProgressReport>()), Times.Exactly(3));
    }

    [Fact]
    public void ScopedInstance_DoesNotReusePreviousChunkValues()
    {
        var services = new ServiceCollection();

        services.AddScoped<IProgressTracker, ProgressTracker>();

        using var provider = services.BuildServiceProvider();
        var fileId = Guid.NewGuid();
        var callback = Mock.Of<IProgress<ProgressReport>>();

        using (var firstAttempt = provider.CreateScope())
        {
            var tracker = firstAttempt.ServiceProvider.GetRequiredService<IProgressTracker>();

            tracker.InitializeFile(fileId, 2);
            tracker.UpdateProgress(fileId, callback, 0, 100);
        }

        using var nextAttempt = provider.CreateScope();
        var nextTracker = nextAttempt.ServiceProvider.GetRequiredService<IProgressTracker>();

        nextTracker.InitializeFile(fileId, 2);

        Assert.Equal(10, nextTracker.UpdateProgress(fileId, callback, 1, 20));
    }
}
