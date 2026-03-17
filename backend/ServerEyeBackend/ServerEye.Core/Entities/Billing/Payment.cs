namespace ServerEye.Core.Entities.Billing;

using System.Diagnostics.CodeAnalysis;
using ServerEye.Core.Enums;

public class Payment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public PaymentProvider Provider { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderPaymentIntentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    
    [SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Stripe API returns string URLs")]
    public string? ReceiptUrl { get; set; }
    
    [SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Stripe API returns string URLs")]
    public string? InvoiceUrl { get; set; }
    
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundedAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
