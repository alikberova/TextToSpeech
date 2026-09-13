using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Dto.Narakeet;
using TextToSpeech.Infra.Services.FileProcessing;
using static TextToSpeech.Infra.TestData;

namespace TextToSpeech.Infra.Stubs;

public sealed class FakeNarakeetHandler : HttpMessageHandler
{
    private const int InProgressStatusCallCount = 5;
    private const string StatusPathPrefix = "/status/";
    private const string ResultPathPrefix = "/result/";

    public Uri BaseAddress { get; } = new("https://fake.narakeet.local/");
    public const int BuildTaskStatusPercentInProgress = 42;

    private readonly BuildTaskStatus _inProgress;
    private readonly byte[] _audioBytes;

    private readonly List<NarakeetVoiceResult> _voices;
    private readonly ConcurrentDictionary<string, int> _statusCallsByTaskId = new();

    public FakeNarakeetHandler()
    {
        _inProgress = new BuildTaskStatus
        {
            Finished = false,
            Succeeded = false,
            Percent = BuildTaskStatusPercentInProgress,
            Message = "processing"
        };

        _audioBytes = AudioFileService.GenerateSilentMp3(2);

        _voices = NarakeetVoices.All
            .Select(FromVoice)
            .ToList();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        var path = uri?.AbsolutePath ?? "";
        if (request.Method == HttpMethod.Get &&
            string.Equals(path.TrimEnd('/'), "/voices", StringComparison.OrdinalIgnoreCase))
        {
            await Delay.RandomShort(cancellationToken);
            return Json(HttpStatusCode.OK, _voices);
        }

        // POST → create task
        if (request.Method == HttpMethod.Post)
        {
            await Delay.RandomShort(cancellationToken);

            var taskId = Guid.NewGuid().ToString("N");
            var buildTask = new BuildTask
            {
                TaskId = taskId,
                RequestId = $"req-{taskId}",
                StatusUrl = new Uri(BaseAddress, $"{StatusPathPrefix}{taskId}").ToString()
            };

            _statusCallsByTaskId[taskId] = 0;

            return Json(HttpStatusCode.OK, buildTask);
        }

        // GET status → in progress for several polls, then finished
        var statusTaskId = GetTaskId(path, StatusPathPrefix);
        if (request.Method == HttpMethod.Get &&
            statusTaskId is not null &&
            _statusCallsByTaskId.ContainsKey(statusTaskId))
        {
            await Delay.RandomShort(cancellationToken);

            var statusCalls = _statusCallsByTaskId.AddOrUpdate(
                statusTaskId,
                1,
                (_, currentStatusCalls) => currentStatusCalls + 1);
            if (statusCalls <= InProgressStatusCallCount)
            {
                return Json(HttpStatusCode.OK, _inProgress);
            }

            return Json(HttpStatusCode.OK, new BuildTaskStatus
            {
                Finished = true,
                Succeeded = true,
                Percent = 100,
                Result = new Uri(BaseAddress, $"{ResultPathPrefix}{statusTaskId}").ToString()
            });
        }

        // GET result → audio bytes
        var resultTaskId = GetTaskId(path, ResultPathPrefix);
        if (request.Method == HttpMethod.Get &&
            resultTaskId is not null &&
            _statusCallsByTaskId.ContainsKey(resultTaskId))
        {
            await Delay.RandomShort(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_audioBytes)
            };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static string? GetTaskId(string path, string pathPrefix)
    {
        if (!path.StartsWith(pathPrefix, StringComparison.Ordinal) || path.Length == pathPrefix.Length)
        {
            return null;
        }

        return path[pathPrefix.Length..];
    }

    private static NarakeetVoiceResult FromVoice(Voice voice)
    {
        return new NarakeetVoiceResult
        {
            Name = voice.ProviderVoiceId,
            Language = voice.Language?.Name ?? string.Empty,
            LanguageCode = voice.Language?.LanguageCode ?? string.Empty,
            Styles = []
        };
    }

    private static HttpResponseMessage Json<T>(HttpStatusCode status, T value) =>
        new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };
}
