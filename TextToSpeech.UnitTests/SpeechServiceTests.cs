using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TextToSpeech.Core.Entities;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Ai;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Services;
using TextToSpeech.Infra.SignalR;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class SpeechServiceTests
{
    private const string FileName = "file.txt";
    private const string FileText = "hello world";
    private const string OwnerId = "owner-1";
    private const string TtsApi = Shared.Narakeet.Key;

    private static readonly Guid AudioFileId = Guid.NewGuid();
    private static readonly byte[] FileBytes = [1, 2, 3];
    private static readonly TtsRequestOptions Request = new()
    {
        Model = "m1",
        Speed = 1,
        ResponseFormat = SpeechResponseFormat.Mp3,
        Voice = new Voice { ProviderVoiceId = "voice-1", Name = "v" }
    };

    [Fact]
    public async Task GetOrInitiateSpeech_WhenRedisHasId_ReturnsId_AndDoesNotEnqueue()
    {
        // Arrange
        var repo = new Mock<IAudioFileRepository>();
        var backgroundQueue = new Mock<IBackgroundTaskQueue>();
        var taskManager = new Mock<ITaskManager>();

        var service = Createservice(repo.Object, backgroundQueue.Object, taskManager.Object);

        // Act
        var resultId = await service.GetOrInitiateSpeech(
            Request,
            FileBytes,
            FileName,
            TtsApi,
            OwnerId);

        // Assert
        Assert.Equal(AudioFileId, resultId);

        backgroundQueue.Verify(
            q => q.QueueBackgroundWorkItem(It.IsAny<Func<CancellationToken, Task>>()),
            Times.Never);

        taskManager.Verify(
            m => m.AddTask(It.IsAny<Guid>(), It.IsAny<CancellationTokenSource>()),
            Times.Never);

        repo.Verify(
            r => r.Add(It.IsAny<AudioFile>()),
            Times.Never);
    }

    private static SpeechService Createservice(IAudioFileRepository? repo = null,
        IBackgroundTaskQueue? backgroundQueue = null,
        ITaskManager? taskManager = null)
    {
        var fileProcessor = new Mock<IFileProcessor>();
        fileProcessor
            .Setup(p => p.ExtractTextAsync(FileBytes))
            .ReturnsAsync(FileText);

        var fileProcessorFactory = new Mock<IFileProcessorFactory>();
        fileProcessorFactory
            .Setup(f => f.GetProcessor(".txt"))
            .Returns(fileProcessor.Object);

        var redis = new Mock<IRedisCacheProvider>();
        redis
            .Setup(r => r.Get<Guid?>(It.IsAny<string>()))
            .ReturnsAsync(AudioFileId);

        var textProcessingService = new Mock<ITextProcessingService>();
        textProcessingService
            .Setup(s => s.SplitTextIfGreaterThan(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(["chunk-1"]);

        return new SpeechService(
            textProcessingService.Object,
            Mock.Of<ITtsServiceFactory>(),
            fileProcessorFactory.Object,
            Mock.Of<IHubContext<AudioHub>>(),
            Mock.Of<ILogger<SpeechService>>(),
            Mock.Of<IMetaDataService>(),
            repo ?? Mock.Of<IAudioFileRepository>(),
            taskManager ?? Mock.Of<ITaskManager>(),
            backgroundQueue ?? Mock.Of<IBackgroundTaskQueue>(),
            Mock.Of<IServiceScopeFactory>(),
            redis.Object);
    }
}
