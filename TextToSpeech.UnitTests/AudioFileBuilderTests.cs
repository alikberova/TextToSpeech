using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Services;
using Xunit;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.UnitTests;

public class AudioFileBuilderTests
{
    private readonly Guid TtsApiId = Shared.OpenAI.Id;
    private const AudioType Type = AudioType.Full;
    private const string FileName = "testfile.mp3";
    private const string InputText = "This is a test.";
    private const string OwnerId = "guest:test-owner";
    private const string ProviderVoiceId = "TestVoice";

    private static Language Language => new("TestLang", "en-US");

    private static TtsRequestOptions TtsRequest => new()
    {
        Voice = CreateVoice(ProviderVoiceId),
        Speed = 1,
        ResponseFormat = SpeechResponseFormat.Mp3,
        Model = "test-model"
    };

    private static Voice CreateVoice(string id, Language? lang = null) => new()
    {
        Name = id,
        ProviderVoiceId = id,
        Language = lang
    };

    [Fact]
    public void Create_ShouldReturnValidAudioFile()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hash = AudioFileBuilder.GenerateHash(InputText, TtsRequest, Type);

        // Act
        var audioFile = AudioFileBuilder.Create(Type, InputText, TtsRequest, OwnerId,
            TtsApiId, FileName, id);

        // Assert
        Assert.Equal(id, audioFile.Id);
        Assert.Equal(FileName, audioFile.FileName);
        Assert.Equal(ProviderVoiceId, audioFile.Voice);
        Assert.Null(audioFile.LanguageCode);
        Assert.Equal(TtsRequest.Speed, audioFile.Speed);
        Assert.Equal(Type, audioFile.Type);
        Assert.Equal(TtsApiId, audioFile.TtsApiId);
        Assert.Equal(OwnerId, audioFile.OwnerId);
        Assert.NotEqual(DateTime.MinValue, audioFile.CreatedAt);
        Assert.NotEmpty(audioFile.Description);
        Assert.Equal(hash, audioFile.Hash);
    }

    [Fact]
    public void Create_AudioFilesShouldHaveSameHash()
    {
        // Arrange, Act
        var audioFile1 = AudioFileBuilder.Create(Type, InputText, TtsRequest, OwnerId);
        var audioFile2 = AudioFileBuilder.Create(Type, InputText, TtsRequest, OwnerId);

        // Assert
        Assert.Equal(audioFile1.Hash, audioFile2.Hash);
    }

    [Fact]
    public void Create_WhenLanguageIsProvided_AffectsHash()
    {
        var request = TtsRequest with { Voice = CreateVoice(ProviderVoiceId, Language) };
        var requestWithOtherLang = request with
        {
            Voice = CreateVoice(ProviderVoiceId, new Language("fr", "fr"))
        };
        var audioFile1 = AudioFileBuilder.Create(Type, InputText, request, OwnerId);
        var audioFile2 = AudioFileBuilder.Create(Type, InputText, request, OwnerId);
        var audioFile3 = AudioFileBuilder.Create(Type, InputText, requestWithOtherLang, OwnerId);

        Assert.Equal(Language.LanguageCode, audioFile1.LanguageCode);

        Assert.Equal(audioFile1.Hash, audioFile2.Hash);
        Assert.NotEqual(audioFile1.Hash, audioFile3.Hash);
    }

    [Fact]
    public void Create_WhenTypeDiffers_AffectsHash()
    {
        var audioFile1 = AudioFileBuilder.Create(AudioType.Sample, InputText, TtsRequest, OwnerId);
        var audioFile2 = AudioFileBuilder.Create(AudioType.Full, InputText, TtsRequest, OwnerId);

        Assert.NotEqual(audioFile1.Hash, audioFile2.Hash);
    }

    [Fact]
    public void Create_AudioFilesShouldHaveDifferentHash()
    {
        // Arrange
        const string change = "change";

        // Act
        var audioFile = AudioFileBuilder.Create(Type, InputText, TtsRequest, OwnerId);

        // Assert
        AssertDifferentHash(audioFile.Hash, TtsRequest with { Voice = CreateVoice(change) });
        AssertDifferentHash(audioFile.Hash, TtsRequest with { Speed = 1.1 });
        AssertDifferentHash(audioFile.Hash, TtsRequest with { ResponseFormat = SpeechResponseFormat.Wav });
        AssertDifferentHash(audioFile.Hash, TtsRequest with { Model = change });
        AssertDifferentHash(audioFile.Hash, TtsRequest with { Model = null });
        AssertDifferentHash(audioFile.Hash, TtsRequest with
        {
            Voice = CreateVoice(ProviderVoiceId, Language with { LanguageCode = change })
        });

        Assert.NotEqual(audioFile.Hash, AudioFileBuilder.Create(
            Type,
            InputText + change,
            TtsRequest,
            OwnerId).Hash);
    }

    private static void AssertDifferentHash(string hash, TtsRequestOptions request)
    {
        Assert.NotEqual(hash, AudioFileBuilder.Create(
            Type,
            InputText,
            request,
            OwnerId).Hash);
    }
}
