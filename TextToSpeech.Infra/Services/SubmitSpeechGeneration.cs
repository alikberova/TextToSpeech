using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Interfaces.Repositories;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Interfaces;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

public sealed class SubmitSpeechGeneration(
    IFileProcessorFactory fileProcessorFactory,
    IAudioFileRepository audioFileRepository,
    ISpeechGenerationRequests requests,
    ISpeechGenerationNotifications notifications) : ISubmitSpeechGeneration
{
    public async Task<Guid> SubmitAsync(TtsRequestOptions request, byte[] fileBytes, string fileName,
        string ttsApi, string ownerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var providerId = Shared.TtsApis.Single(kv => kv.Key.Equals(ttsApi, StringComparison.OrdinalIgnoreCase)).Value;
        var fileProcessor = fileProcessorFactory.GetProcessor(Path.GetExtension(fileName)) ??
            throw new NotSupportedException("File type not supported");
        var fileText = await fileProcessor.ExtractTextAsync(fileBytes);
        cancellationToken.ThrowIfCancellationRequested();

        var hash = AudioFileBuilder.GenerateHash(fileText, request, AudioType.Full);
        var existingAudio = await audioFileRepository.GetCompletedByHash(hash, ownerId, providerId);

        if (existingAudio is not null)
        {
            _ = notifications.PublishAsync(existingAudio.Value, ownerId, Status.Completed);
            return existingAudio.Value;
        }

        var provider = Shared.TtsApis.Single(x => x.Value == providerId).Key;
        var input = new SpeechGenerationInput(Guid.NewGuid(), ownerId, fileName, fileText, provider, request);
        var accepted = await requests.AcceptAsync(input, fileBytes, cancellationToken);

        notifications.Refresh(ownerId);

        return accepted.InputId;
    }
}
