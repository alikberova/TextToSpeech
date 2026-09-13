using System.Text.Json;
using System.Text.Json.Serialization;
using TextToSpeech.Core.Models;

namespace TextToSpeech.Core.Converters;

public sealed class SpeechResponseJsonConverter : JsonConverter<SpeechResponseFormat>
{
    public override SpeechResponseFormat Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var str = reader.GetString();

        if (SpeechResponseFormat.TryParse(str, out var value))
        {
            return value;
        }

        throw new JsonException(SpeechResponseFormat.UnsupportedFormatError(str));
    }

    public override void Write(Utf8JsonWriter writer, SpeechResponseFormat value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
