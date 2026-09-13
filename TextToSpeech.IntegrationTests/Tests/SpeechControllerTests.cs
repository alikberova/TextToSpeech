using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TextToSpeech.Api;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using Xunit.Abstractions;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.IntegrationTests.Tests;

public class SpeechControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private const string AudioMpeg = "audio/mpeg";

    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public SpeechControllerTests(TestWebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = factory.HttpClient;
    }

    [Theory]
    [InlineData(Shared.OpenAI.Key)]
    [InlineData(Shared.Narakeet.Key)]
    [InlineData(Shared.ElevenLabs.Key)]
    public async Task GetVoiceSample_ReturnsMp3Sample(string ttsApi)
    {
        await Authenticate();

        // Arrange
        var httpContent = new StringContent(
            JsonSerializer.Serialize(SpeechRequestGenerator.GenerateFakeSpeechRequest(ttsApi)),
            Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/speech/sample", httpContent);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(AudioMpeg, response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData(Shared.OpenAI.Key)]
    [InlineData(Shared.Narakeet.Key)]
    [InlineData(Shared.ElevenLabs.Key)]
    public async Task CreateSpeech_ReturnsMp3(string ttsApi)
    {
        // Arrange
        var token = await Authenticate();
        var hubConnection = BuildHubConnection(_client, _factory, token);

        var statusCapture = new SpeechStatusCapture();

        hubConnection.On<Guid, string, int?, string?>(Shared.AudioStatusUpdated, statusCapture.OnStatusUpdatedAsync);

        _output.WriteLine("Starting hub connection...");

        await hubConnection.StartAsync();

        _output.WriteLine("Hub connection started");

        // Act
        var responseFileId = await SubmitSpeechAsync(ttsApi, statusCapture);
        var downloadBytes = await DownloadSpeechAsync(responseFileId);

        //Assert 

        AssertCompletedSpeech(statusCapture, responseFileId);
        Assert.NotEmpty(downloadBytes);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    private static HubConnection BuildHubConnection(
        HttpClient client,
        TestWebApplicationFactory<Program> factory,
        string? token)
    {
        return new HubConnectionBuilder()
            .WithUrl($"{client.BaseAddress!.OriginalString}{Shared.AudioHubEndpoint}", options =>
            {
                options.Transports = HttpTransportType.LongPolling;

                options.AccessTokenProvider = () => Task.FromResult(token);

                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();
    }

    private async Task<Guid> SubmitSpeechAsync(string ttsApi, SpeechStatusCapture statusCapture)
    {
        var response = await _client.PostAsync("/api/speech", GetFormData(ttsApi));
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        Assert.True(
            Guid.TryParse(responseString.Trim('"'), out var responseFileId),
            "Response string file ID is not a valid guid");

        statusCapture.ExpectFileId(responseFileId);

        var completedTask = await Task.WhenAny(
            statusCapture.StatusUpdated,
            Task.Delay(TimeSpan.FromSeconds(20)));

        Assert.True(completedTask == statusCapture.StatusUpdated, "Timed out to update speech status");

        return responseFileId;
    }

    private async Task<byte[]> DownloadSpeechAsync(Guid fileId)
    {
        var downloadResp = await _client.GetAsync($"/api/audio/download/{fileId}");
        downloadResp.EnsureSuccessStatusCode();

        Assert.Equal(AudioMpeg, downloadResp.Content.Headers.ContentType?.MediaType);

        return await downloadResp.Content.ReadAsByteArrayAsync();
    }

    private static void AssertCompletedSpeech(SpeechStatusCapture statusCapture, Guid responseFileId)
    {
        Assert.Equal(Status.Completed.ToString(), statusCapture.Status);
        Assert.Equal(responseFileId, statusCapture.FileId);
        Assert.Null(statusCapture.ErrorMessage);
        Assert.NotEmpty(statusCapture.ProgressReports);
    }

    private static MultipartFormDataContent GetFormData(string ttsApi)
    {
        var speechRequest = SpeechRequestGenerator.GenerateFakeSpeechRequest(ttsApi, true);
        var voice = speechRequest.TtsRequestOptions.Voice;

        var fileContent = new StreamContent(speechRequest.File!.OpenReadStream());
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = nameof(speechRequest.File),
            FileName = speechRequest.File.FileName
        };

        var formData = new MultipartFormDataContent
        {
            { new StringContent(speechRequest.TtsApi), nameof(speechRequest.TtsApi) },
            { new StringContent(speechRequest.LanguageCode!), nameof(speechRequest.LanguageCode) },
            { fileContent, nameof(speechRequest.File), speechRequest.File.FileName }
        };

        AddTtsRequestOptions(formData, speechRequest, voice);
        AddVoiceLanguage(formData, speechRequest, voice);

        return formData;
    }

    private static void AddTtsRequestOptions(
        MultipartFormDataContent formData,
        TtsRequest speechRequest,
        Voice voice)
    {
        formData.Add(
            new StringContent(speechRequest.TtsRequestOptions.Model ?? string.Empty),
            $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Model)}");
        formData.Add(
            new StringContent(voice.Name),
            $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}.{nameof(voice.Name)}");
        formData.Add(
            new StringContent(voice.ProviderVoiceId),
            $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}." +
            $"{nameof(voice.ProviderVoiceId)}");
        formData.Add(
            new StringContent(((int)voice.QualityTier).ToString(CultureInfo.InvariantCulture)),
            $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}." +
            $"{nameof(voice.QualityTier)}");
        formData.Add(
            new StringContent(speechRequest.TtsRequestOptions.Speed.ToString(CultureInfo.InvariantCulture)),
            $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Speed)}");
        formData.Add(
            new StringContent(speechRequest.TtsRequestOptions.ResponseFormat.ToString()),
            $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.ResponseFormat)}");
    }

    private static void AddVoiceLanguage(
        MultipartFormDataContent formData,
        TtsRequest speechRequest,
        Voice voice)
    {
        if (voice.Language is not null)
        {
            formData.Add(
                new StringContent(voice.Language.Name),
                $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}." +
                $"{nameof(voice.Language)}.{nameof(voice.Language.Name)}");
            formData.Add(
                new StringContent(voice.Language.LanguageCode),
                $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}." +
                $"{nameof(voice.Language)}.{nameof(voice.Language.LanguageCode)}");
        }
    }

    private async Task<string> Authenticate()
    {
        var resp = await _client.PostAsync("/api/auth/guest", new StringContent(string.Empty));
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();

        var token = JsonDocument.Parse(json)
            .RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token), "Guest token missing");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return token;
    }

    private sealed class SpeechStatusCapture
    {
        private readonly TaskCompletionSource<bool> _statusUpdated = new();
        private readonly TaskCompletionSource<Guid> _expectedFileId = new();

        public Task<bool> StatusUpdated => _statusUpdated.Task;
        public string Status { get; private set; } = string.Empty;
        public string? ErrorMessage { get; private set; }
        public Guid? FileId { get; private set; }
        public List<int?> ProgressReports { get; } = [];

        public void ExpectFileId(Guid fileId)
        {
            _expectedFileId.SetResult(fileId);
        }

        public async Task OnStatusUpdatedAsync(
            Guid fileId,
            string status,
            int? progressPercentage,
            string? errorMessage)
        {
            if (fileId != await _expectedFileId.Task)
            {
                return;
            }

            Status = status;
            ErrorMessage = errorMessage;
            FileId = fileId;

            if (progressPercentage.HasValue)
            {
                ProgressReports.Add(progressPercentage);
            }

            if (status == TextToSpeech.Core.Enums.Status.Completed.ToString())
            {
                _statusUpdated.SetResult(true);
            }
        }
    }
}
