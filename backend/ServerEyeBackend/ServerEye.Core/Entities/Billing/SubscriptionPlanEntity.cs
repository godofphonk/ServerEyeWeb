namespace ServerEye.Core.Entities.Billing;

using ServerEye.Core.Enums;

public class SubscriptionPlanEntity
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
    public Dictionary<string, string> Features { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
