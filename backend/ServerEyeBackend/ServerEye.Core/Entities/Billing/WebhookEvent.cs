namespace ServerEye.Core.Entities.Billing;

using ServerEye.Core.Enums;

public enum WebhookEventStatus
{
    Received,
    Processing,
    Processed,
    Failed,
    DeadLetter
}

public class WebhookEvent
{
    public Guid Id { get; set; }
    public PaymentProvider Provider { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public string? Headers { get; set; }
    public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Received;
    public string? ProcessingError { get; set; }
    public int ProcessingAttempts { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
