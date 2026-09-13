using Microsoft.Extensions.Logging;
using Moq;
using OpenAI;
using OpenAI.Audio;
using System.ClientModel;
using System.ClientModel.Primitives;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Services.Ai;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class OpenAiServiceTests
{
    [Fact]
    public async Task GetVoices_ReturnsStaticVoiceList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var voices = await service.GetVoices();

        // Assert
        Assert.NotNull(voices);
        Assert.True(voices.Count >= 11);
        Assert.Contains(voices, v => v.Name == "Alloy" && v.ProviderVoiceId == "alloy");
        Assert.Contains(voices, v => v.Name == "Nova" && v.ProviderVoiceId == "nova");
        Assert.Contains(voices, v => v.Name == "Onyx" && v.ProviderVoiceId == "onyx");
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_ReturnsEmptyArray_WhenNoTextChunks()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RequestSpeechChunksAsync([], Guid.NewGuid(), TestData.TtsRequestOptions,
            Mocks.ProgressCallback, default);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_ThrowsTaskCanceled_WhenTokenCanceledBeforeWork()
    {
        // Arrange
        var service = CreateService();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act / Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            service.RequestSpeechChunksAsync(
                ["text"],
                Guid.NewGuid(),
                TestData.TtsRequestOptions,
                Mocks.ProgressCallback,
                cts.Token));
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_Reports100PercentPerChunk()
    {
        var fileId = Guid.NewGuid();

        var progressContext = Mocks.CreateProgressContext(fileId);

        var service = CreateService(CreateOpenAIClientMock(), progressContext.TrackerMock.Object);

        var textChunks = new List<string> { "first", "second" };

        var result = await service.RequestSpeechChunksAsync(textChunks, fileId, TestData.TtsRequestOptions,
            progressContext.Progress, CancellationToken.None);

        Assert.Equal(textChunks.Count, result.Length);
        Assert.All(result, bytes => Assert.False(bytes.IsEmpty));
        Assert.Equal(new[] { 100, 100 }, progressContext.ReportedPercentages);

        progressContext.TrackerMock.Verify(p => p.InitializeFile(fileId, textChunks.Count), Times.Once);
        progressContext.TrackerMock.Verify(
            p => p.UpdateProgress(fileId, progressContext.Progress, It.IsAny<int>(), 100),
            Times.Exactly(textChunks.Count));
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_UsesParallelExecution()
    {
        var chunks = new List<string> { "chunk-1", "chunk-2", "chunk-3" };
        var fileId = Guid.NewGuid();

        IReadOnlyList<string>? capturedItems = null;
        int capturedMaxParallel = -1;
        Func<string, int, Task>? capturedAction = null;
        CancellationToken capturedToken = default;

        var parallelExecutionServiceMock = new Mock<IParallelExecutionService>();

        parallelExecutionServiceMock
            .Setup(x => x.RunTasksFromItems(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int>(),
                It.IsAny<Func<string, int, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback((IReadOnlyList<string> items,
                    int maxParallel,
                    Func<string, int, Task> action,
                    CancellationToken token) =>
            {
                capturedItems = items;
                capturedMaxParallel = maxParallel;
                capturedAction = action;
                capturedToken = token;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService(parallelExecutionServiceMock: parallelExecutionServiceMock);

        var result = await service.RequestSpeechChunksAsync(chunks, fileId, TestData.TtsRequestOptions,
            Mocks.ProgressCallback, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(chunks.Count, result.Length);
        Assert.Same(chunks, capturedItems);
        Assert.Equal(20, capturedMaxParallel);
        Assert.NotNull(capturedAction);
        Assert.Equal(CancellationToken.None, capturedToken);
    }

    private static OpenAiService CreateService(OpenAIClient? openAIClient = null,
        IProgressTracker? progressTracker = null,
        Mock<IParallelExecutionService>? parallelExecutionServiceMock = null)
    {
        return new OpenAiService(openAIClient ?? new OpenAIClient("test-key"),
            Mock.Of<ILogger<OpenAiService>>(),
            progressTracker ?? Mocks.ProgressTracker,
            parallelExecutionServiceMock?.Object ?? Mocks.ParallelExecutionService);
    }

    private static OpenAIClient CreateOpenAIClientMock()
    {
        var mockResult = new Mock<ClientResult<BinaryData>>(
            BinaryData.FromBytes([1, 2, 3]),
            Mock.Of<PipelineResponse>());

        mockResult.SetupGet(r => r.Value).Returns(BinaryData.FromBytes([1, 2, 3]));

        var audioClientMock = new Mock<AudioClient>();

        audioClientMock
            .Setup(c => c.GenerateSpeechAsync(
                It.IsAny<string>(),
                It.IsAny<GeneratedSpeechVoice>(),
                It.IsAny<SpeechGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult.Object);

        var openAiClientMock = new Mock<OpenAIClient>();

        openAiClientMock
            .Setup(c => c.GetAudioClient(It.IsAny<string>()))
            .Returns(audioClientMock.Object);

        return openAiClientMock.Object;
    }
}
