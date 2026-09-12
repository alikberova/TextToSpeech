using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        var httpContent = new StringContent(JsonSerializer.Serialize(SpeechRequestGenerator.GenerateFakeSpeechRequest(ttsApi)),
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

        var spechStatusUpdated = new TaskCompletionSource<bool>();
        var expectedFileId = new TaskCompletionSource<Guid>();

        var status = string.Empty;
        string? errorMessage = null;
        Guid? fileId = null;
        var progressReports = new List<int?>();

        hubConnection.On<Guid, string, int?, string?>(Shared.AudioStatusUpdated, async (fileIdResult, updatedStatus, progressPercentage, errorMessageResult) =>
        {
            if (fileIdResult != await expectedFileId.Task)
            {
                return;
            }

            status = updatedStatus;
            errorMessage = errorMessageResult;
            fileId = fileIdResult;
            if (progressPercentage.HasValue)
            {
                progressReports.Add(progressPercentage);
            }
            if (updatedStatus == Status.Completed.ToString())
            {
                spechStatusUpdated.SetResult(true);
            }
        });

        _output.WriteLine("Starting hub connection...");

        await hubConnection.StartAsync();

        _output.WriteLine("Hub connection started");

        // Act
        var response = await _client.PostAsync("/api/speech", GetFormData(ttsApi));
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.True(Guid.TryParse(responseString.Trim('"'), out var respStringFileId), "Response string file ID is not a valid guid");
        expectedFileId.SetResult(respStringFileId);

        var completedTask = await Task.WhenAny(spechStatusUpdated.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.True(completedTask == spechStatusUpdated.Task, "Timed out to update speech status");

        var downloadResp = await _client.GetAsync($"/api/audio/download/{respStringFileId}");
        downloadResp.EnsureSuccessStatusCode();
        var downloadBytes = await downloadResp.Content.ReadAsByteArrayAsync();

        //Assert 

        Assert.Equal(Status.Completed.ToString(), status);
        Assert.Equal(respStringFileId, fileId);
        Assert.Null(errorMessage);
        Assert.NotEmpty(progressReports);

        Assert.Equal(AudioMpeg, downloadResp.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(downloadBytes);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    private static HubConnection BuildHubConnection(HttpClient client, TestWebApplicationFactory<Program> factory, string? token)
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
            { new StringContent(speechRequest.TtsRequestOptions.Model ?? string.Empty), $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Model)}" },
            { new StringContent(voice.Name), $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}.{nameof(voice.Name)}" },
            { new StringContent(voice.ProviderVoiceId), $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}.{nameof(voice.ProviderVoiceId)}" },
            { new StringContent(((int)voice.QualityTier).ToString(CultureInfo.InvariantCulture)), $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}.{nameof(voice.QualityTier)}" },
            { new StringContent(speechRequest.TtsRequestOptions.Speed.ToString(CultureInfo.InvariantCulture)), $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Speed)}" },
            { new StringContent(speechRequest.TtsRequestOptions.ResponseFormat.ToString()), $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.ResponseFormat)}" },
            { fileContent, nameof(speechRequest.File), speechRequest.File.FileName }
        };

        if (voice.Language is not null)
        {
            formData.Add(new StringContent(voice.Language.Name), $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}.{nameof(voice.Language)}.{nameof(voice.Language.Name)}");
            formData.Add(new StringContent(voice.Language.LanguageCode), $"{nameof(speechRequest.TtsRequestOptions)}.{nameof(TtsRequestOptions.Voice)}.{nameof(voice.Language)}.{nameof(voice.Language.LanguageCode)}");
        }

        return formData;
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
}
