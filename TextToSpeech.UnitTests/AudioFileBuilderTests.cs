using TextToSpeech.Core.Models;
using TextToSpeech.Infra.Constants;
using TextToSpeech.Infra.Services;
using Xunit;
using static TextToSpeech.Core.Enums;

namespace TextToSpeech.UnitTests;

public class AudioFileBuilderTests
{
    private readonly byte[] Bytes = [1, 2, 3];
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
        var audioFile = AudioFileBuilder.Create(Bytes, Type, InputText, TtsRequest, OwnerId,
            TtsApiId, FileName, id);

        // Assert
        Assert.Equal(id, audioFile.Id);
        Assert.Equal(FileName, audioFile.FileName);
        Assert.Equal(Bytes, audioFile.Data);
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
    public void Create_AudioFilesShouldBeEqual()
    {
        // Arrange, Act
        var audioFile1 = AudioFileBuilder.Create(Bytes, Type, InputText, TtsRequest, OwnerId);
        var audioFile2 = AudioFileBuilder.Create(Bytes, Type, InputText, TtsRequest, OwnerId);

        // Assert
        Assert.True(audioFile1 == audioFile2);
        Assert.Equal(audioFile1, audioFile2);
    }

    [Fact]
    public void Create_WhenLanguageIsProvided_AffectsEquality()
    {
        var request = TtsRequest with { Voice = CreateVoice(ProviderVoiceId, Language) };
        var requestWithOtherLang = request with
        {
            Voice = CreateVoice(ProviderVoiceId, new Language("fr", "fr"))
        };
        var audioFile1 = AudioFileBuilder.Create(Bytes, Type, InputText, request, OwnerId);
        var audioFile2 = AudioFileBuilder.Create(Bytes, Type, InputText, request, OwnerId);
        var audioFile3 = AudioFileBuilder.Create(Bytes, Type, InputText, requestWithOtherLang, OwnerId);

        Assert.Equal(Language.LanguageCode, audioFile1.LanguageCode);

        Assert.True(audioFile1 == audioFile2);
        Assert.Equal(audioFile1, audioFile2);

        Assert.False(audioFile1 == audioFile3);
        Assert.NotEqual(audioFile1, audioFile3);
    }

    [Fact]
    public void Create_WhenTypeDiffers_AffectsEquality()
    {
        var audioFile1 = AudioFileBuilder.Create(Bytes, AudioType.Sample, InputText, TtsRequest, OwnerId);
        var audioFile2 = AudioFileBuilder.Create(Bytes, AudioType.Full, InputText, TtsRequest, OwnerId);

        Assert.False(audioFile1 == audioFile2);
        Assert.NotEqual(audioFile1, audioFile2);
    }

    [Fact]
    public void Create_AudioFilesShouldNotBeEqual()
    {
        // Arrange
        const string change = "change";

        // Act
        var audioFile = AudioFileBuilder.Create(Bytes, Type, InputText, TtsRequest, OwnerId);

        // Assert
        Assert.NotEqual(audioFile, AudioFileBuilder.Create(
            Bytes,
            Type,
            InputText,
            TtsRequest with { Voice = CreateVoice(change) },
            OwnerId));

        Assert.NotEqual(audioFile, AudioFileBuilder.Create(
            Bytes,
            Type,
            InputText,
            TtsRequest with { Speed = 1.1 },
            OwnerId));

        Assert.NotEqual(audioFile, AudioFileBuilder.Create(
            Bytes,
            Type,
            InputText,
            TtsRequest with { ResponseFormat = SpeechResponseFormat.Wav },
            OwnerId));

        Assert.NotEqual(audioFile, AudioFileBuilder.Create(
            Bytes,
            Type,
            InputText,
            TtsRequest with { Model = change },
            OwnerId));

        Assert.NotEqual(audioFile, AudioFileBuilder.Create(
            Bytes,
            Type,
            InputText,
            TtsRequest with { Model = null },
            OwnerId));

        Assert.NotEqual(audioFile, AudioFileBuilder.Create(
            [1, 2],
            Type,
            InputText,
            TtsRequest,
            OwnerId));

        Assert.NotEqual(audioFile, AudioFileBuilder.Create(
            Bytes,
            Type,
            InputText,
            TtsRequest with
            {
                Voice = CreateVoice(ProviderVoiceId, Language with { LanguageCode = change })
            },
            OwnerId));

        Assert.NotEqual(audioFile, AudioFileBuilder.Create(
            Bytes,
            Type,
            InputText + change,
            TtsRequest,
            OwnerId));
    }
}
