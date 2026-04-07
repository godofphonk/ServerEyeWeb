namespace ServerEye.Core.Entities.Billing;

public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
