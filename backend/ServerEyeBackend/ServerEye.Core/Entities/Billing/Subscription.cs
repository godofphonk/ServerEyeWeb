namespace ServerEye.Core.Entities.Billing;

using ServerEye.Core.Enums;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public SubscriptionPlan PlanType { get; set; }
    public SubscriptionStatus Status { get; set; }
    public PaymentProvider Provider { get; set; }
    public string? ProviderCustomerId { get; set; }
    public string? ProviderSubscriptionId { get; set; }
    public string? ProviderPriceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public bool IsYearly { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? TrialStart { get; set; }
    public DateTime? TrialEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
