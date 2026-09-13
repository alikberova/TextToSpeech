using System.Text.Json.Serialization;

namespace TextToSpeech.Infra.Dto.Narakeet;

public sealed record BuildTaskStatus
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("percent")]
    public int Percent { get; init; }

    [JsonPropertyName("succeeded")]
    public bool Succeeded { get; init; }

    [JsonPropertyName("finished")]
    public bool Finished { get; init; }

    [JsonPropertyName("result")]
    public string Result { get; init; } = string.Empty;
}
