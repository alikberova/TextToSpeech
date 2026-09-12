using Microsoft.Extensions.Logging;
using Moq;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Services.Ai;
using TextToSpeech.Infra.Stubs;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class ElevenLabsServiceTests
{
    [Fact]
    public async Task GetVoices_ReturnsMappedVoices()
    {
        var voices = new Voice[]
        {
            new() { Name = "Voice A", ProviderVoiceId = "v-1"},
            new() { Name = "Voice B", ProviderVoiceId = "v-2"}
        };

        var service = CreateService(voicesResponse: voices);

        var result = await service.GetVoices();

        Assert.NotNull(result);
        Assert.Collection(result,
            v =>
            {
                Assert.Equal("Voice A", v.Name);
                Assert.Equal("v-1", v.ProviderVoiceId);
            },
            v =>
            {
                Assert.Equal("Voice B", v.Name);
                Assert.Equal("v-2", v.ProviderVoiceId);
            });
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_ReturnsEmpty_WhenNoChunks()
    {
        var service = CreateService();

        var result = await service.RequestSpeechChunksAsync([], Guid.NewGuid(), TestData.TtsRequestOptions, Mocks.ProgressCallback, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_ThrowsWhenCanceled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var service = CreateService();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            service.RequestSpeechChunksAsync(["text"], Guid.NewGuid(), TestData.TtsRequestOptions, Mocks.ProgressCallback, cts.Token));
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_Reports100PercentPerChunk()
    {
        var fileId = Guid.NewGuid();
        var progressContext = Mocks.CreateProgressContext(fileId);

        var service = CreateService(progressContext);

        var textChunks = new List<string> { "first", "second" };

        var result = await service.RequestSpeechChunksAsync(textChunks, fileId, TestData.TtsRequestOptions,
            progressContext.Progress, CancellationToken.None);

        Assert.Equal(textChunks.Count, result.Length);
        Assert.All(result, bytes => Assert.False(bytes.IsEmpty));

        progressContext.TrackerMock.Verify(p => p.InitializeFile(fileId, textChunks.Count), Times.Once);
        progressContext.TrackerMock.Verify(p => p.UpdateProgress(fileId, progressContext.Progress, It.IsAny<int>(), 100), Times.Exactly(textChunks.Count));
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

    private static ElevenLabsService CreateService(ProgressTrackerContext? progressContext = null,
        Mock<IParallelExecutionService>? parallelExecutionServiceMock = null,
        Voice[]? voicesResponse = null)
    {
        progressContext ??= Mocks.CreateProgressContext(Guid.NewGuid());

        return new ElevenLabsService(
            FakeElevenLabsClient.Create(voicesResponse: voicesResponse),
            Mock.Of<ILogger<ElevenLabsService>>(),
            progressContext.TrackerMock.Object,
            parallelExecutionServiceMock?.Object ?? Mocks.ParallelExecutionService);
    }
}
