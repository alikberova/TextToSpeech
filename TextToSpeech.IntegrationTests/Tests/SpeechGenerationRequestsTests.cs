using System.Text;
using Microsoft.EntityFrameworkCore;
using Moq;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Jobs;
using TextToSpeech.Infra.Services;
using TextToSpeech.Infra.Storage;

namespace TextToSpeech.IntegrationTests.Tests;

public sealed class SpeechGenerationRequestsTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task AcceptedRequest_CanBeLoadedAndReusedAfterReopeningStorage()
    {
        var directory = Directory.CreateTempSubdirectory();
        var paths = Mock.Of<IPathService>(p => p.GetFileStoragePath() == directory.FullName);
        var request = SpeechRequestGenerator.GenerateFakeSpeechRequest(Shared.Narakeet.Key);
        var input = new SpeechGenerationInput(Guid.NewGuid(), Guid.NewGuid().ToString(), "source.txt",
            request.Input!, request.TtsApi, request.TtsRequestOptions);
        // The original file retains its UTF-8 BOM; extracted text does not.
        var source = Encoding.UTF8.GetBytes("\uFEFF" + input.FileText);
        JobAcceptance accepted;

        try
        {
            await using (var context = database.CreateContext())
            {
                await context.Database.MigrateAsync();

                var requests = new SpeechGenerationRequests(context, new SubmitBackgroundJob(context),
                    new FileSystemArtifactStorage(paths));
                accepted = await requests.AcceptAsync(input, source, CancellationToken.None);
            }

            await using var reopened = database.CreateContext();
            var storage = new FileSystemArtifactStorage(paths);
            var reloadedRequests = new SpeechGenerationRequests(reopened, new SubmitBackgroundJob(reopened),
                storage);
            var loaded = await reloadedRequests.LoadAsync(accepted.InputId, CancellationToken.None);
            var duplicate = await reloadedRequests.AcceptAsync(input with { FileId = Guid.NewGuid() }, source,
                CancellationToken.None);

            Assert.Equivalent(input, loaded);
            Assert.Equal(accepted.JobId, duplicate.JobId);
            Assert.Equal(accepted.InputId, duplicate.InputId);
            Assert.False(duplicate.Created);

            await AssertSourceStoredAsync(reopened, storage, accepted.InputId, source);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static async Task AssertSourceStoredAsync(
        AppDbContext context,
        IArtifactStorage storage,
        Guid inputId,
        byte[] source)
    {
        FormattableString sourceQuery = $"""
            SELECT "SourceId" AS "Value"
            FROM "SpeechGenerationRequest"
            WHERE "Id" = {inputId}
            """;
        var sourceId = await context.Database
            .SqlQuery<Guid>(sourceQuery)
            .SingleAsync();

        Assert.Equal(source, await storage.ReadBytesAsync(sourceId, CancellationToken.None));
    }
}
