using Bogus;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;
using TextToSpeech.Api;
using TextToSpeech.Core.Models;
using TextToSpeech.Infra;
using TextToSpeech.Infra.Constants;

namespace TextToSpeech.IntegrationTests;

internal static class SpeechRequestGenerator
{
    private static readonly Faker faker = new ();

    public static TtsRequest GenerateFakeSpeechRequest(string ttsApi, bool addFile = false)
    {
        var ttsOptions = new TtsRequestOptions
        {
            Model = GetModelByTtsApi(ttsApi),
            Voice = GetVoiceByTtsApi(ttsApi),
            Speed = Math.Round(faker.Random.Double(0.5, 2.0), 1),
            ResponseFormat = SpeechResponseFormat.Mp3
        };

        var speechRequest = new TtsRequest
        {
            TtsApi = ttsApi,
            LanguageCode = "en",
            Input = faker.Lorem.Sentence(),
            TtsRequestOptions = ttsOptions,
            File = addFile ? CreateFakeFile() : null
        };

        return speechRequest;
    }

    private static IFormFile CreateFakeFile()
    {
        const string content = "Fake file content";
        const string fileName = "fakefile.txt";

        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.Length).Returns(stream.Length);
        fileMock.Setup(f => f.ContentType).Returns("text/plain");

        return fileMock.Object;
    }

    private static Voice GetVoiceByTtsApi(string ttsApi)
    {
        return ttsApi switch
        {
            Shared.OpenAI.Key => faker.PickRandom(TestData.OpenAiVoices.All),
            Shared.Narakeet.Key => faker.PickRandom(TestData.NarakeetVoices.All),
            Shared.ElevenLabs.Key => faker.PickRandom(TestData.ElevenLabsVoices.All),
            _ => throw new ArgumentException($"Unsupported TTS API: {ttsApi}", nameof(ttsApi)),
        };
    }

    private static string? GetModelByTtsApi(string ttsApi)
    {
        return ttsApi switch
        {
            Shared.OpenAI.Key => "gpt-4o-mini-tts",
            Shared.ElevenLabs.Key => "eleven_multilingual_v2",
            _ => null,
        };
    }
}
