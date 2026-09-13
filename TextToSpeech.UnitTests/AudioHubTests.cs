using Microsoft.AspNetCore.SignalR;
using Moq;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.SignalR;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class AudioHubTests
{
    private readonly Mock<ITaskManager> _taskManagerMock;
    private readonly AudioHub _audioHub;
    private readonly Mock<HubCallerContext> _mockContext;

    public AudioHubTests()
    {
        _taskManagerMock = new Mock<ITaskManager>();
        _audioHub = new AudioHub(_taskManagerMock.Object);
        _mockContext = new Mock<HubCallerContext>();
    }

    [Fact]
    public async Task CancelProcessing_ShouldInvokeTaskManager()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _taskManagerMock.Setup(tm => tm.TryCancelTask(fileId))
            .Returns(Task.FromResult(true));
        _audioHub.Context = _mockContext.Object;

        // Act
        await _audioHub.CancelProcessing(fileId);

        // Assert
        _taskManagerMock.Verify(tm => tm.TryCancelTask(fileId), Times.Once);
    }
}
