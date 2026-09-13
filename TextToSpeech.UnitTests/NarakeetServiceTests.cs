using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Infra.Dto.Narakeet;
using TextToSpeech.Infra.Services.Ai;
using TextToSpeech.Infra.Stubs;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class NarakeetServiceTests
{
    [Fact]
    public async Task GetVoices_ReturnsMappedVoices_WhenApiResponds()
    {
        // Arrange
        var apiResult = new List<NarakeetVoiceResult>
        {
            new() { Name = "voice-a", Language = "English", LanguageCode = "en" },
            new() { Name = "voice-b", Language = "French", LanguageCode = "fr" }
        };

        var (service, _) = CreateService(apiResult);

        // Act
        var voices = await service.GetVoices();

        // Assert
        Assert.NotNull(voices);
        Assert.Collection(voices,
            v =>
            {
                Assert.Equal("voice-a", v.Name);
                Assert.Equal("voice-a", v.ProviderVoiceId);
                Assert.Equal("en", v.Language?.LanguageCode);
                Assert.Equal("English", v.Language?.Name);
            },
            v =>
            {
                Assert.Equal("voice-b", v.Name);
                Assert.Equal("voice-b", v.ProviderVoiceId);
                Assert.Equal("fr", v.Language?.LanguageCode);
                Assert.Equal("French", v.Language?.Name);
            });
    }

    [Fact]
    public async Task GetVoices_ReturnsNull_WhenApiReturnsEmptyList()
    {
        // Arrange
        var apiResult = new List<NarakeetVoiceResult>();

        var (service, _) = CreateService(apiResult);

        // Act
        var voices = await service.GetVoices();

        // Assert
        Assert.Null(voices);
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_ReturnsEmptyArray_WhenNoTextChunks()
    {
        // Arrange
        var (service, handler) = CreateService([], MockBehavior.Strict);

        // Act
        var result = await service.RequestSpeechChunksAsync([], Guid.NewGuid(), TestData.TtsRequestOptions, Mocks.ProgressCallback, default);

        // Assert
        Assert.Empty(result);

        handler.Protected().Verify("SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_ThrowsOperationCanceled_WhenTokenCanceledBeforeRequests()
    {
        // Arrange
        var (service, handler) = CreateService([], MockBehavior.Strict);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act / Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.RequestSpeechChunksAsync(["text"], Guid.NewGuid(), TestData.TtsRequestOptions, Mocks.ProgressCallback, cts.Token));

        handler.Protected().Verify("SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RequestSpeechChunksAsync_ReportsInternalPercentToTracker()
    {
        var fileId = Guid.NewGuid();
        var progressContext = Mocks.CreateProgressContext(fileId);
        var inProgressPercent = FakeNarakeetHandler.BuildTaskStatusPercentInProgress;

        NarakeetService service = CreateService(progressContext.TrackerMock.Object, new FakeNarakeetHandler());

        var result = await service.RequestSpeechChunksAsync(["chunk"], fileId, TestData.TtsRequestOptions, progressContext.Progress, default);

        Assert.Single(result);
        Assert.False(result[0].IsEmpty);
        progressContext.TrackerMock.Verify(t => t.InitializeFile(fileId, 1), Times.Once);
        progressContext.TrackerMock.Verify(t =>
            t.UpdateProgress(fileId, progressContext.Progress, 0, inProgressPercent), Times.AtLeastOnce);
        Assert.Contains(inProgressPercent, progressContext.ReportedPercentages);
    }

    private static (NarakeetService service, Mock<HttpMessageHandler> handler) CreateService(
        List<NarakeetVoiceResult> apiResult,
        MockBehavior behavior = MockBehavior.Loose)
    {
        var httpHandler = new Mock<HttpMessageHandler>(behavior);

        httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(apiResult)
            });

        var service = CreateService(Mocks.ProgressTracker, httpHandler.Object);

        return (service, httpHandler);
    }

    private static NarakeetService CreateService(IProgressTracker progressTracker, HttpMessageHandler httpMessageHandler)
    {
        var httpClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("https://example.com")
        };

        var service = new NarakeetService(httpClient,
            progressTracker,
            Mock.Of<ILogger<NarakeetService>>());

        return service;
    } 
}
