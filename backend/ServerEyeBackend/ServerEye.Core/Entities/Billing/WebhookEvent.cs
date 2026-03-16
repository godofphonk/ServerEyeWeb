namespace ServerEye.Core.Entities.Billing;

using ServerEye.Core.Enums;

public class WebhookEvent
{
    public Guid Id { get; set; }
    public PaymentProvider Provider { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public string? ProcessingError { get; set; }
    public int ProcessingAttempts { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
