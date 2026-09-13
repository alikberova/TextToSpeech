namespace TextToSpeech.Infra.Dto;

public sealed record class EmailRequest
{
    public string Name { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
