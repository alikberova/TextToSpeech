using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs;
using TextToSpeech.Worker;

namespace TextToSpeech.IntegrationTests.Tests;

public sealed class WorkerTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    private const string JobType = "test-job";

    [Fact]
    public async Task RunningJob_RejectsDuplicateExecution_AndObservesDurableCancellation()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new Mock<IBackgroundJobHandler>();

        handler.SetupGet(x => x.InputVersion).Returns(1);
        handler.Setup(x => x.ExecuteAsync(It.IsAny<JobExecution>(), It.IsAny<IProgress<int>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (JobExecution _, IProgress<int> _, CancellationToken token) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            });

        using var services = CreateServices(handler.Object);
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TextToSpeech.Infra.AppDbContext>();

        await context.Database.MigrateAsync();

        var submission = new JobSubmission(JobType, 1, Guid.NewGuid(), Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid());
        var accepted = await new SubmitBackgroundJob(context).SubmitAsync(submission, CancellationToken.None);
        var message = new JobDispatchMessage(
            accepted.JobId, JobDispatchMessage.CurrentVersion, submission.CorrelationId);
        var runner = services.GetRequiredService<JobRunner>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var execution = runner.RunAsync(message, timeout.Token);

        await started.Task.WaitAsync(timeout.Token);
        Assert.True(await runner.RunAsync(message, timeout.Token));
        handler.Verify(x => x.ExecuteAsync(It.IsAny<JobExecution>(), It.IsAny<IProgress<int>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobs>();

        Assert.True(await jobs.RequestCancellationAsync(accepted.JobId, submission.OwnerId, timeout.Token));
        Assert.True(await execution.WaitAsync(timeout.Token));
        Assert.Equal(JobStatus.Cancelled,
            (await jobs.GetAsync(accepted.JobId, submission.OwnerId, timeout.Token))!.Status);
    }

    private ServiceProvider CreateServices(IBackgroundJobHandler handler)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(Options.Create(new WorkerConfig
        {
            Concurrency = 2,
            LeaseSeconds = 60,
            HeartbeatSeconds = 1,
            RecoveryIntervalSeconds = 5
        }));
        services.AddScoped(_ => database.CreateContext());
        services.AddBackgroundJobs();
        services.AddScoped<WorkerJobReader>();
        services.AddSingleton<JobHeartbeat>();
        services.AddSingleton<JobRunner>();
        services.AddKeyedSingleton(JobType, handler);

        return services.BuildServiceProvider();
    }
}
