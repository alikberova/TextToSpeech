using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Security.Claims;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.SignalR;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class AudioHubTests
{
    private const string OwnerId = "owner-1";
    private readonly Mock<ICancellationRegistry> _taskManagerMock;
    private readonly AudioHub _audioHub;
    private readonly Mock<HubCallerContext> _mockContext;

    public AudioHubTests()
    {
        _taskManagerMock = new Mock<ICancellationRegistry>();
        _audioHub = new AudioHub(_taskManagerMock.Object);
        _mockContext = new Mock<HubCallerContext>();
    }

    [Fact]
    public async Task CancelProcessing_ShouldInvokeTaskManager()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _mockContext.SetupGet(context => context.User)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, OwnerId)])));
        _taskManagerMock.Setup(tm => tm.TryCancelTask(fileId, OwnerId))
            .Returns(Task.FromResult(true));
        _audioHub.Context = _mockContext.Object;

        // Act
        await _audioHub.CancelProcessing(fileId);

        // Assert
        _taskManagerMock.Verify(tm => tm.TryCancelTask(fileId, OwnerId), Times.Once);
    }
}
