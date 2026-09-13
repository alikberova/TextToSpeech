using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Entities;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra.Services;

public sealed class SpeechGenerationRequests(
    AppDbContext context,
    ISubmitBackgroundJob submitJob,
    IArtifactStorage storage) : ISpeechGenerationRequests
{
    public const string JobType = "speech-generation";
    public const int InputVersion = 1;

    public Task<JobAcceptance> AcceptAsync(
        SpeechGenerationInput input,
        byte[] source,
        CancellationToken cancellationToken)
    {
        var optionsJson = JsonSerializer.Serialize(input.Options);
        var fingerprintData = JsonSerializer.Serialize(new
        {
            Provider = input.TtsApi,
            input.FileName,
            input.FileText,
            Options = optionsJson,
            SourceHash = Convert.ToHexString(SHA256.HashData(source))
        });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintData)));

        return context.InTransactionAsync(async () =>
        {
            var accepted = await submitJob.SubmitAsync(new JobSubmission(
                JobType,
                InputVersion,
                input.FileId,
                input.OwnerId,
                fingerprint,
                fingerprint,
                Guid.NewGuid()), cancellationToken);

            if (!accepted.Created)
            {
                return accepted;
            }

            var sourceId = await storage.WriteAsync(source, cancellationToken);
            var textId = await storage.WriteAsync(Encoding.UTF8.GetBytes(input.FileText), cancellationToken);
            var request = CreateRequest(input, accepted, sourceId, textId, optionsJson);

            context.SpeechGenerationRequests.Add(request);
            await context.SaveChangesAsync(cancellationToken);

            // Failed/uncertain database commits can leave unreferenced objects. Do not delete objects
            // here: a commit may have succeeded. Storage cleanup must reconcile persisted references.
            return accepted;
        }, cancellationToken);
    }

    public async Task<SpeechGenerationInput> LoadAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var request = await context.SpeechGenerationRequests
            .AsNoTracking()
            .SingleAsync(x => x.Id == fileId, cancellationToken);

        if (request.Version != InputVersion)
        {
            throw new NotSupportedException("Unsupported speech request version.");
        }

        var options = JsonSerializer.Deserialize<TtsRequestOptions>(request.OptionsJson)
            ?? throw new InvalidOperationException("Speech request options are missing.");
        var text = Encoding.UTF8.GetString(await storage.ReadBytesAsync(request.TextId, cancellationToken));

        return new SpeechGenerationInput(
            request.Id,
            request.OwnerId,
            request.FileName,
            text,
            request.Provider,
            options);
    }

    private static SpeechGenerationRequest CreateRequest(
        SpeechGenerationInput input,
        JobAcceptance accepted,
        Guid sourceId,
        Guid textId,
        string optionsJson)
    {
        return new SpeechGenerationRequest
        {
            Id = accepted.InputId,
            JobId = accepted.JobId,
            OwnerId = input.OwnerId,
            FileName = input.FileName,
            Provider = input.TtsApi,
            Version = InputVersion,
            SourceId = sourceId,
            TextId = textId,
            OptionsJson = optionsJson
        };
    }

    public Task<Guid?> GetJobIdAsync(Guid fileId, string ownerId, CancellationToken cancellationToken) =>
        context.SpeechGenerationRequests
            .Where(x => x.Id == fileId && x.OwnerId == ownerId)
            .Select(x => (Guid?)x.JobId)
            .SingleOrDefaultAsync(cancellationToken);
}
