using System.Text.Json;
using TextToSpeech.Core.Models;
using Xunit;

namespace TextToSpeech.UnitTests;

public sealed class SpeechResponseJsonConverterTests
{
    [Fact]
    public void Deserialize_ValidString_ReturnsExpectedFormat()
    {
        var json = $"\"{SpeechResponseFormat.Mp3}\"";
        var result = JsonSerializer.Deserialize<SpeechResponseFormat>(json);

        Assert.Equal(SpeechResponseFormat.Mp3, result);
    }

    [Fact]
    public void Deserialize_InvalidString_ThrowsJsonException()
    {
        var json = $"\"{SpeechResponseFormat.Mp3.ToString().ToUpperInvariant()}\"";

        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<SpeechResponseFormat>(json));

        var expected = SpeechResponseFormat.UnsupportedFormatError(json.Trim('"'));
        Assert.Equal(expected, ex.Message);
    }
}
