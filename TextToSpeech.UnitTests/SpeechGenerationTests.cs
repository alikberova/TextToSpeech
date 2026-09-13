using Moq;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Core.Jobs;
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
        var service = CreateSubmission(repository.Object, dispatcher.Object, Mock.Of<ISpeechGenerationRequests>());

        var result = await service.SubmitAsync(options, SourceBytes, FileName,
            Shared.Narakeet.Key, OwnerId, CancellationToken.None);

        Assert.Equal(audioId, result);
    }

    [Fact]
    public async Task Submit_NoOwnedResult_DispatchesIndependentOperation()
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

        var dispatcher = new Mock<ISpeechGenerationDispatcher>();
        var service = CreateSubmission(Mock.Of<IAudioFileRepository>(), dispatcher.Object, requests.Object);

        var id = await service.SubmitAsync(options, SourceBytes, FileName,
            Shared.Narakeet.Key, OwnerId, CancellationToken.None);

        Assert.Equal(acceptance.InputId, id);
        dispatcher.Verify(d => d.DispatchAsync(acceptance.JobId, id, OwnerId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Submit_AfterDispatchFailure_QueuesTheSamePendingJobAgain()
    {
        var accepted = new JobAcceptance(Guid.NewGuid(), Guid.NewGuid(), true);
        var requests = new Mock<ISpeechGenerationRequests>();

        requests.SetupSequence(r =>
                r.AcceptAsync(It.IsAny<SpeechGenerationInput>(), SourceBytes, CancellationToken.None))
            .ReturnsAsync(accepted)
            .ReturnsAsync(accepted with { Created = false });

        var jobs = new Mock<IBackgroundJobs>();

        jobs.Setup(j => j.GetAsync(accepted.JobId, OwnerId, CancellationToken.None))
            .ReturnsAsync(new JobSnapshot(accepted.JobId, SpeechGenerationRequests.JobType,
                SpeechGenerationRequests.InputVersion, accepted.InputId, Guid.NewGuid(), JobStatus.Pending,
                0, false, null, null));

        var dispatcher = new Mock<ISpeechGenerationDispatcher>();

        dispatcher.SetupSequence(d =>
                d.DispatchAsync(accepted.JobId, accepted.InputId, OwnerId, CancellationToken.None))
            .ThrowsAsync(new OperationCanceledException())
            .Returns(Task.CompletedTask);

        var service = CreateSubmission(
            Mock.Of<IAudioFileRepository>(),
            dispatcher.Object,
            requests.Object,
            jobs.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SubmitAsync(TestData.TtsRequestOptions,
            SourceBytes, FileName, Shared.Narakeet.Key, OwnerId, CancellationToken.None));

        var id = await service.SubmitAsync(TestData.TtsRequestOptions, SourceBytes, FileName,
            Shared.Narakeet.Key, OwnerId, CancellationToken.None);

        Assert.Equal(accepted.InputId, id);
        dispatcher.Verify(d => d.DispatchAsync(accepted.JobId, id, OwnerId, CancellationToken.None), Times.Exactly(2));
    }

    private static SubmitSpeechGeneration CreateSubmission(IAudioFileRepository repository,
        ISpeechGenerationDispatcher dispatcher, ISpeechGenerationRequests requests,
        IBackgroundJobs? jobs = null)
    {
        var processor = new Mock<IFileProcessor>();
        processor.Setup(p => p.ExtractTextAsync(SourceBytes)).ReturnsAsync(TestData.Text1500chars);
        var processors = new Mock<IFileProcessorFactory>();
        processors.Setup(p => p.GetProcessor(".txt")).Returns(processor.Object);

        return new SubmitSpeechGeneration(processors.Object, repository, dispatcher, requests,
            jobs ?? Mock.Of<IBackgroundJobs>(),
            Mock.Of<ISpeechGenerationNotifications>());
    }
}
