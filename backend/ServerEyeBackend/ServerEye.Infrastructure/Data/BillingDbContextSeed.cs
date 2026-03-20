namespace ServerEye.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;

public static class BillingDbContextSeed
{
    public static async Task SeedAsync(BillingDbContext context)
    {
        await context.Database.MigrateAsync();

        // Check if plans already exist
        if (await context.SubscriptionPlans.AnyAsync())
        {
            return; // Database already seeded
        }

        var plans = new List<SubscriptionPlanEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PlanType = SubscriptionPlan.Free,
                Name = "Pro",
                Description = "Advanced features for professional teams",
                MonthlyPrice = 9.99m,
                YearlyPrice = 99.99m,
                MaxServers = 10,
                MetricsRetentionDays = 30,
                HasAlerts = true,
                HasApiAccess = true,
                HasPrioritySupport = false,
                Features = new Dictionary<string, string>
                {
                    ["maxAlerts"] = "100",
                    ["webhooks"] = "false"
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                PlanType = SubscriptionPlan.Free,
                Name = "Free",
                Description = "Perfect for getting started with server monitoring",
                MonthlyPrice = 0,
                YearlyPrice = 0,
                MaxServers = 3,
                MetricsRetentionDays = 7,
                HasAlerts = true,
                HasApiAccess = false,
                HasPrioritySupport = false,
                Features = new Dictionary<string, string>
                {
                    ["maxAlerts"] = "10",
                    ["webhooks"] = "false",
                    ["slackIntegration"] = "false",
                    ["emailSupport"] = "false"
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                PlanType = SubscriptionPlan.Pro,
                Name = "Enterprise",
                Description = "Complete solution for large organizations",
                MonthlyPrice = 29.99m,
                YearlyPrice = 299.99m,
                MaxServers = 50,
                MetricsRetentionDays = 90,
                HasAlerts = true,
                HasApiAccess = true,
                HasPrioritySupport = true,
                Features = new Dictionary<string, string>
                {
                    ["maxAlerts"] = "1000",
                    ["webhooks"] = "true",
                    ["slackIntegration"] = "true",
                    ["emailSupport"] = "true",
                    ["phoneSupport"] = "false",
                    ["customReports"] = "true"
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Only seed if no plans exist
        try
        {
            if (!await context.SubscriptionPlans.AnyAsync())
            {
                await context.SubscriptionPlans.AddRangeAsync(plans);
                await context.SaveChangesAsync();
            }
        }
        catch
        {
            // If table doesn't exist or other error, skip seeding
            // Data might already be there via SQL script
        }
    }
}
