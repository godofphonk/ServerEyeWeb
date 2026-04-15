namespace ServerEye.Core.Configuration.Plans;

using ServerEye.Core.Enums;

/// <summary>
/// Static definitions of subscription plans.
/// These are the source of truth for plan configurations and are synced to the database on startup.
/// </summary>
public static class SubscriptionPlanDefinitions
{
    public static readonly SubscriptionPlanDefinition Free = new()
    {
        Id = Guid.Parse("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c"),
        PlanType = SubscriptionPlan.Free,
        Name = "Free",
        Description = "Basic monitoring for single server",
        MonthlyPrice = 0,
        YearlyPrice = 0,
        MaxServers = 1,
        MetricsRetentionDays = 7,
        HasAlerts = false,
        HasApiAccess = false,
        HasPrioritySupport = false,
        Features = new List<string> { "basic alerts", "up to 3 alerts" }
    };

    public static readonly SubscriptionPlanDefinition Lite = new()
    {
        Id = Guid.Parse("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"),
        PlanType = SubscriptionPlan.Lite,
        Name = "Lite",
        Description = "Affordable monitoring for growing projects",
        MonthlyPrice = 4.99m,
        YearlyPrice = 49.99m,
        MaxServers = 5,
        MetricsRetentionDays = 30,
        HasAlerts = true,
        HasApiAccess = false,
        HasPrioritySupport = false,
        Features = new List<string> { "Basic alerts", "up to 10 active alerts settings" }
    };

    public static readonly SubscriptionPlanDefinition Pro = new()
    {
        Id = Guid.Parse("841bb3db-424c-46e5-a752-04641391c993"),
        PlanType = SubscriptionPlan.Pro,
        Name = "Pro",
        Description = "Advanced monitoring for multiple servers",
        MonthlyPrice = 9.99m,
        YearlyPrice = 99.99m,
        MaxServers = 10,
        MetricsRetentionDays = 30,
        HasAlerts = true,
        HasApiAccess = true,
        HasPrioritySupport = false,
        Features = new List<string> { "Custom alerts", "Unlimited alerts" }
    };

    public static readonly SubscriptionPlanDefinition Enterprise = new()
    {
        Id = Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
        PlanType = SubscriptionPlan.Enterprise,
        Name = "Enterprise",
        Description = "Enterprise-grade monitoring with unlimited servers",
        MonthlyPrice = 50,
        YearlyPrice = 500,
        MaxServers = -1,
        MetricsRetentionDays = 90,
        HasAlerts = true,
        HasApiAccess = true,
        HasPrioritySupport = true,
        Features = new List<string> { "support@servereye.dev" }
    };

    /// <summary>
    /// Get all plan definitions.
    /// </summary>
    public static List<SubscriptionPlanDefinition> GetAll()
    {
        return new List<SubscriptionPlanDefinition>
        {
            Free,
            Lite,
            Pro,
            Enterprise
        };
    }

    /// <summary>
    /// Get plan definition by ID.
    /// </summary>
    public static SubscriptionPlanDefinition? GetById(Guid id)
    {
        return GetAll().FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// Get plan definition by plan type enum.
    /// </summary>
    public static SubscriptionPlanDefinition? GetByType(SubscriptionPlan planType)
    {
        return GetAll().FirstOrDefault(p => p.PlanType == planType);
    }
}
