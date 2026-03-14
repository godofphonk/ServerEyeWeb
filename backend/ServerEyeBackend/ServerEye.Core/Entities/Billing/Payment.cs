namespace ServerEye.Core.Entities.Billing;

using ServerEye.Core.Enums;

public class Payment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public PaymentProvider Provider { get; set; }
    public string? ProviderPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public PaymentType Type { get; set; }
    public string? Description { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Subscription? Subscription { get; set; }
}