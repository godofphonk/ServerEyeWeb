namespace ServerEye.Core.Configuration.Plans;

using ServerEye.Core.Enums;

/// <summary>
/// Definition of a subscription plan with all configurable properties.
/// These definitions are the source of truth and are synced to the database on application startup.
/// </summary>
public class SubscriptionPlanDefinition
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
