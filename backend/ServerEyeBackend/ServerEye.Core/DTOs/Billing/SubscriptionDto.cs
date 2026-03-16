namespace ServerEye.Core.DTOs.Billing;

using ServerEye.Core.Enums;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public SubscriptionPlan PlanType { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public bool IsYearly { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? TrialEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSubscriptionRequest
{
    public SubscriptionPlan PlanType { get; set; }
    public bool IsYearly { get; set; }
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class CreateSubscriptionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionUrl { get; set; } = string.Empty;
}

public class UpdateSubscriptionRequest
{
    public SubscriptionPlan NewPlanType { get; set; }
    public bool IsYearly { get; set; }
}

public class CancelSubscriptionRequest
{
    public bool CancelImmediately { get; set; }
    public string? CancellationReason { get; set; }
}

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public SubscriptionPlan PlanType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MaxServers { get; set; }
    public int MetricsRetentionDays { get; set; }
    public bool HasAlerts { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasPrioritySupport { get; set; }
    public List<string> Features { get; set; } = new();
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentStatus Status { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? InvoiceUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePaymentIntentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CreatePaymentIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
}
