namespace TextToSpeech.Infra.Jobs.Persistence;

internal sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public int ContractVersion { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
