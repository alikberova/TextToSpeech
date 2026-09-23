using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Security.Claims;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.SignalR;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class AudioHubTests
{
    private const string OwnerId = "owner-1";
    private readonly AudioHub _audioHub;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<ISpeechGenerationRequests> _requests = new();
    private readonly Mock<IBackgroundJobs> _jobs = new();

    public AudioHubTests()
    {
        _audioHub = new AudioHub(new SpeechJobConnections(), _requests.Object, _jobs.Object);
        _mockContext = new Mock<HubCallerContext>();
    }

    [Fact]
    public async Task CancelProcessing_PersistsOwnerCancellation()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        _requests.Setup(r => r.GetJobIdAsync(fileId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(jobId);
        _jobs.Setup(j => j.RequestCancellationAsync(jobId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockContext.SetupGet(context => context.User)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, OwnerId)])));
        _audioHub.Context = _mockContext.Object;

        // Act
        await _audioHub.CancelProcessing(fileId);

        // Assert
        _jobs.Verify(j => j.RequestCancellationAsync(jobId, OwnerId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
