using System.Threading.Channels;
using TextToSpeech.Core.Interfaces;
using TextToSpeech.Core.Models;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.Infra.Services;

internal sealed class SpeechGenerationProgress : IProgress<ProgressReport>
{
    private readonly Channel<ProgressReport> _reports = Channel.CreateBounded<ProgressReport>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Report(ProgressReport value)
    {
        _reports.Writer.TryWrite(value);
    }

    public void Complete()
    {
        _reports.Writer.TryComplete();
    }

    public async Task PublishAsync(Guid fileId, string ownerId, ISpeechGenerationNotifications notifications)
    {
        var lastProgress = -1;

        await foreach (var progressPercentage in _reports.Reader.ReadAllAsync()
            .Select(report => report.ProgressPercentage))
        {
            if (progressPercentage > lastProgress)
            {
                await notifications.PublishAsync(fileId, ownerId, Status.Processing, progressPercentage);
                lastProgress = progressPercentage;
            }
        }
    }
}
