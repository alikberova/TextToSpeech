using ElevenLabs;
using System.Net;
using System.Text;
using System.Text.Json;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Services.FileProcessing;
using static TextToSpeech.Infra.TestData;

namespace TextToSpeech.Infra.Stubs;

public static class FakeElevenLabsClient
{
    public static ElevenLabsClient Create(byte[]? audioBytesResponse = null, Voice[]? voicesResponse = null)
    {
        audioBytesResponse ??= AudioFileService.GenerateSilentMp3(2);
        voicesResponse ??= ElevenLabsVoices.All;

        var handler = FakeElevenLabsHandler.WithResponses(audioBytesResponse, voicesResponse);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = handler.BaseAddress
        };

        return new ElevenLabsClient(
            new ElevenLabsAuthentication("test-key"),
            new ElevenLabsClientSettings(handler.BaseAddress.ToString()),
            httpClient);
    }
}

sealed class FakeElevenLabsHandler : HttpMessageHandler
{
    private readonly byte[] _audioBytes;
    private readonly string _voicesResponse;

    public Uri BaseAddress { get; } = new("https://fake.elevenlabs.local/");

    private FakeElevenLabsHandler(string voicesResponse, byte[] audioBytes)
    {
        _voicesResponse = voicesResponse;
        _audioBytes = audioBytes;
    }

    public static FakeElevenLabsHandler WithResponses(byte[] audioBytes, Voice[] voices)
    {
        var voicesPayload = new
        {
            voices = voices.Select(v => new
            {
                voice_id = v.ProviderVoiceId,
                name = v.Name
            })
        };

        var voicesResponse = JsonSerializer.Serialize(voicesPayload);

        return new FakeElevenLabsHandler(voicesResponse, audioBytes);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.Contains("voices", StringComparison.OrdinalIgnoreCase))
        {
            await Delay.RandomShort(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_voicesResponse, Encoding.UTF8, "application/json")
            };
        }

        if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.Contains("text-to-speech", StringComparison.OrdinalIgnoreCase))
        {
            await Delay.RandomMedium(cancellationToken);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_audioBytes)
            };

            response.Headers.TryAddWithoutValidation("history-item-id", "history-item-id-1");

            return response;
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
