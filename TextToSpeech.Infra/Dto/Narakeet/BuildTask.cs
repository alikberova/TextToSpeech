using System.Text.Json.Serialization;

namespace TextToSpeech.Infra.Dto.Narakeet;

public sealed record BuildTask
{
    [JsonPropertyName("statusUrl")]
    public string StatusUrl { get; init; } = string.Empty;

    [JsonPropertyName("taskId")]
    public string TaskId { get; init; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; init; } = string.Empty;
}
