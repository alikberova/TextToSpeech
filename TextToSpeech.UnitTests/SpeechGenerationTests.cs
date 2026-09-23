using Moq;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Services;
using Xunit;
using static TextToSpeech.Core.Enums;
using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.UnitTests;

public sealed class SpeechGenerationTests
{
    private const string OwnerId = "owner-1";
    private const string FileName = "file.txt";
    private static readonly byte[] SourceBytes = [1];

    [Fact]
    public async Task Submit_ExistingOwnedResult_DoesNotCreateJob()
    {
        var options = TestData.TtsRequestOptions;
        var audioId = Guid.NewGuid();
        var hash = AudioFileBuilder.GenerateHash(TestData.Text1500chars, options, AudioType.Full);
        var repository = new Mock<IAudioFileRepository>();
        repository.Setup(r => r.GetCompletedByHash(hash, OwnerId, Shared.Narakeet.Id)).ReturnsAsync(audioId);
        var service = CreateSubmission(repository.Object, Mock.Of<ISpeechGenerationRequests>(MockBehavior.Strict));

        var result = await service.SubmitAsync(options, SourceBytes, FileName,
            Shared.Narakeet.Key, OwnerId, CancellationToken.None);

        Assert.Equal(audioId, result);
    }

    [Fact]
    public async Task Submit_NoOwnedResult_AcceptsDurableRequest()
    {
        var options = TestData.TtsRequestOptions;
        var acceptance = new JobAcceptance(Guid.NewGuid(), Guid.NewGuid(), true);
        var requests = new Mock<ISpeechGenerationRequests>();

        requests.Setup(r => r.AcceptAsync(It.Is<SpeechGenerationInput>(input =>
            input.OwnerId == OwnerId &&
            input.FileName == FileName &&
            input.FileText == TestData.Text1500chars &&
            input.TtsApi == Shared.Narakeet.Key &&
            input.Options == options), SourceBytes, CancellationToken.None)).ReturnsAsync(acceptance);

        var service = CreateSubmission(Mock.Of<IAudioFileRepository>(), requests.Object);

        var id = await service.SubmitAsync(options, SourceBytes, FileName,
            Shared.Narakeet.Key, OwnerId, CancellationToken.None);

        Assert.Equal(acceptance.InputId, id);
        requests.VerifyAll();
    }

    private static SubmitSpeechGeneration CreateSubmission(IAudioFileRepository repository,
        ISpeechGenerationRequests requests)
    {
        var processor = new Mock<IFileProcessor>();
        processor.Setup(p => p.ExtractTextAsync(SourceBytes)).ReturnsAsync(TestData.Text1500chars);
        var processors = new Mock<IFileProcessorFactory>();
        processors.Setup(p => p.GetProcessor(".txt")).Returns(processor.Object);

        return new SubmitSpeechGeneration(processors.Object, repository, requests,
            Mock.Of<ISpeechGenerationNotifications>());
    }
}
