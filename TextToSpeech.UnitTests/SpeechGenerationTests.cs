using Moq;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using TextToSpeech.Infra.Services;
using Xunit;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.UnitTests;

public sealed class SpeechGenerationTests
{
    private const string OwnerId = "owner-1";
    private const string FileName = "file.txt";
    private static readonly byte[] SourceBytes = [1];

    [Fact]
    public async Task Submit_ExistingOwnedResult_DoesNotDispatch()
    {
        var options = TestData.TtsRequestOptions;
        var audioId = Guid.NewGuid();
        var hash = AudioFileBuilder.GenerateHash(TestData.Text1500chars, options, AudioType.Full);
        var repository = new Mock<IAudioFileRepository>();
        repository.Setup(r => r.GetCompletedByHash(hash, OwnerId, Shared.Narakeet.Id)).ReturnsAsync(audioId);
        var dispatcher = new Mock<ISpeechGenerationDispatcher>(MockBehavior.Strict);
        var service = CreateSubmission(repository.Object, dispatcher.Object);

        var result = await service.SubmitAsync(options, SourceBytes, FileName,
            Shared.Narakeet.Key, OwnerId, CancellationToken.None);

        Assert.Equal(audioId, result);
    }

    [Fact]
    public async Task Submit_NoOwnedResult_DispatchesIndependentOperation()
    {
        var options = TestData.TtsRequestOptions;
        var dispatcher = new Mock<ISpeechGenerationDispatcher>();
        var service = CreateSubmission(Mock.Of<IAudioFileRepository>(), dispatcher.Object);

        var id = await service.SubmitAsync(options, SourceBytes, FileName,
            Shared.Narakeet.Key, OwnerId, CancellationToken.None);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<SpeechGenerationInput>(input =>
            input.FileId == id && input.OwnerId == OwnerId && input.FileName == FileName &&
            input.FileText == TestData.Text1500chars && input.TtsApi == Shared.Narakeet.Key && input.Options == options),
            CancellationToken.None), Times.Once);
    }

    private static SubmitSpeechGeneration CreateSubmission(IAudioFileRepository repository,
        ISpeechGenerationDispatcher dispatcher)
    {
        var processor = new Mock<IFileProcessor>();
        processor.Setup(p => p.ExtractTextAsync(SourceBytes)).ReturnsAsync(TestData.Text1500chars);
        var processors = new Mock<IFileProcessorFactory>();
        processors.Setup(p => p.GetProcessor(".txt")).Returns(processor.Object);
        return new SubmitSpeechGeneration(processors.Object, repository, dispatcher,
            Mock.Of<ISpeechGenerationNotifications>());
    }
}
