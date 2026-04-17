namespace ServerEye.Core.DTOs.Billing;

using ServerEye.Core.Enums;

/// <summary>
/// DTO representing a user's subscription plan limits and current usage.
/// </summary>
public class UserPlanLimitsDto
{
    /// <summary>
    /// Maximum number of servers allowed by the user's plan.
    /// -1 indicates unlimited (Enterprise).
    /// </summary>
    public int MaxServers { get; init; }

    /// <summary>
    /// Current number of servers the user has.
    /// </summary>
    public int CurrentServers { get; init; }

    /// <summary>
    /// Number of days metrics are retained.
    /// </summary>
    public int MetricsRetentionDays { get; init; }

    /// <summary>
    /// The user's current plan type.
    /// </summary>
    public SubscriptionPlan PlanType { get; init; }

    /// <summary>
    /// The name of the user's current plan.
    /// </summary>
    public string PlanName { get; init; } = string.Empty;

    /// <summary>
    /// Whether the user has an active subscription.
    /// </summary>
    public bool HasActiveSubscription { get; init; }
}
