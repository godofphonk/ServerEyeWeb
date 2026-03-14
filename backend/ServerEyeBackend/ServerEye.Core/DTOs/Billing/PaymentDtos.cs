namespace ServerEye.Core.DTOs.Billing;

using ServerEye.Core.Enums;

public record CreateCustomerResult(bool Success, string? CustomerId, string? Error = null);

public record CreatePaymentIntentRequest(
    Guid UserId,
    decimal Amount,
    string Currency,
    string? Description = null,
    Dictionary<string, object>? Metadata = null
);

public record CreatePaymentIntentResult(
    bool Success,
    string? PaymentIntentId,
    string? ClientSecret,
    string? Error = null
);

public record CreateSubscriptionRequest(
    string CustomerId,
    Guid PlanId,
    string? PaymentMethodId = null,
    int? TrialDays = null,
    Dictionary<string, object>? Metadata = null
);

public record CreateSubscriptionResult(
    bool Success,
    string? SubscriptionId,
    string? ClientSecret,
    SubscriptionStatus? Status,
    string? Error = null
);

public record UpdateSubscriptionRequest(
    Guid NewPlanId,
    bool ProrationBehavior = true
);

public record UpdateSubscriptionResult(
    bool Success,
    string? SubscriptionId,
    string? Error = null
);

public record CancelSubscriptionResult(
    bool Success,
    string? SubscriptionId,
    DateTime? CanceledAt,
    string? Error = null
);

public record CreateCheckoutSessionRequest(
    Guid UserId,
    Guid PlanId,
    string SuccessUrl,
    string CancelUrl,
    int? TrialDays = null,
    Dictionary<string, object>? Metadata = null
);

public record CreateCheckoutSessionResult(
    bool Success,
    string? SessionId,
    string? CheckoutUrl,
    string? Error = null
);

public record AttachPaymentMethodResult(
    bool Success,
    string? PaymentMethodId,
    string? Error = null
);

public record DetachPaymentMethodResult(
    bool Success,
    string? Error = null
);

public record SetDefaultPaymentMethodResult(
    bool Success,
    string? Error = null
);

public record RefundPaymentResult(
    bool Success,
    string? RefundId,
    decimal? Amount,
    string? Error = null
);

public record WebhookEventResult(
    bool Success,
    string? EventType,
    string? Error = null
);

public record CreatePaymentRequest(
    Guid UserId,
    decimal Amount,
    string Currency,
    PaymentType Type,
    Guid? SubscriptionId = null,
    string? Description = null
);

public record PaymentEventDto(
    string EventType,
    string PaymentId,
    Guid UserId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string? FailureReason = null
);

public record SubscriptionEventDto(
    string EventType,
    string SubscriptionId,
    Guid UserId,
    SubscriptionStatus Status,
    DateTime? CurrentPeriodEnd = null,
    DateTime? CanceledAt = null
);

public record SubscriptionLimits(
    int MaxServers,
    int MetricsRetentionDays,
    bool HasAlerts,
    bool HasApiAccess,
    bool HasPrioritySupport
);
