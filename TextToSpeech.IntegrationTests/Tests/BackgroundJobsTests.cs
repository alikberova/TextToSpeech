using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs;

namespace TextToSpeech.IntegrationTests.Tests;

public sealed class BackgroundJobsTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database = new();

    [Fact]
    public async Task Submit_ConcurrentRequests_CreateOneJobAndDispatch()
    {
        var submission = CreateSubmission();
        await using var firstContext = _database.CreateContext();
        await using var secondContext = _database.CreateContext();
        var first = new SubmitBackgroundJob(firstContext);
        var second = new SubmitBackgroundJob(secondContext);

        var results = await Task.WhenAll(
            first.SubmitAsync(submission, CancellationToken.None),
            second.SubmitAsync(submission with { InputId = Guid.NewGuid() }, CancellationToken.None));

        Assert.Equal(results[0].JobId, results[1].JobId);
        Assert.Equal(results[0].InputId, results[1].InputId);
        Assert.Single(results, x => x.Created);

        var dispatchCount = await firstContext.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM jobs.\"OutboxMessage\"")
            .SingleAsync();

        Assert.Equal(1, dispatchCount);
    }

    [Fact]
    public async Task ExpiredWorker_CannotCompleteAfterRecoveryAndRetry()
    {
        await using var context = _database.CreateContext();
        var jobs = new BackgroundJobs(context);
        var submitJob = new SubmitBackgroundJob(context);
        var submission = CreateSubmission();
        var accepted = await submitJob.SubmitAsync(submission, CancellationToken.None);

        var lease = TimeSpan.FromMinutes(1);
        var original = await jobs.TryStartAsync(accepted.JobId, lease, CancellationToken.None);

        Assert.NotNull(original);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE jobs.\"BackgroundJob\" SET \"LeaseExpiresAt\" = clock_timestamp() - interval '1 second' WHERE \"Id\" = {accepted.JobId}");
        Assert.True(await jobs.RecoverExpiredAsync(accepted.JobId, CancellationToken.None));
        Assert.Null(await jobs.TryStartAsync(accepted.JobId, lease, CancellationToken.None));
        Assert.True(await jobs.ResolveRecoveryAsync(accepted.JobId, true, CancellationToken.None));

        var retry = await jobs.TryStartAsync(accepted.JobId, lease, CancellationToken.None);

        Assert.NotNull(retry);

        var resultId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            jobs.CompleteAsync(accepted.JobId, retry.AttemptId, resultId, CancellationToken.None));

        await using var transaction = await context.Database.BeginTransactionAsync();

        Assert.False(await jobs.CompleteAsync(accepted.JobId, original.AttemptId, resultId, CancellationToken.None));
        Assert.True(await jobs.CompleteAsync(accepted.JobId, retry.AttemptId, resultId, CancellationToken.None));
        await transaction.CommitAsync();

        var result = await jobs.GetAsync(accepted.JobId, submission.OwnerId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(JobStatus.Completed, result.Status);
        Assert.Equal(resultId, result.ResultId);
    }

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        await using var context = _database.CreateContext();

        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _database.DisposeAsync();

    private static JobSubmission CreateSubmission() => new(
        "speech",
        1,
        Guid.NewGuid(),
        Guid.NewGuid().ToString(),
        Guid.NewGuid().ToString(),
        Guid.NewGuid().ToString(),
        Guid.NewGuid());
}
