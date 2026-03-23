namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly ILogger<SubscriptionService> logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        ILogger<SubscriptionService> logger)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.logger = logger;
    }

    public async Task<SubscriptionDto?> GetUserSubscriptionAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null)
        {
            return null;
        }

        // For now, return hardcoded free plan info since we don't have plan details in the entity
        return new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            PlanType = SubscriptionPlan.Free, // Default to free for now
            PlanName = "Free",
            Status = subscription.Status,
            Amount = 0,
            Currency = "usd",
            IsYearly = false,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CanceledAt = null,
            TrialEnd = null,
            CreatedAt = subscription.CreatedAt
        };
    }

    public async Task<CreateSubscriptionResponse> CreateSubscriptionCheckoutAsync(
        Guid userId,
        CreateSubscriptionRequest request)
    {
        throw new NotImplementedException("Paid subscriptions not implemented yet");
    }

    public async Task<SubscriptionDto> UpdateSubscriptionPlanAsync(
        Guid userId,
        UpdateSubscriptionRequest request)
    {
        throw new NotImplementedException("Subscription updates not implemented yet");
    }

    public async Task CancelSubscriptionAsync(
        Guid userId,
        CancelSubscriptionRequest request)
    {
        throw new NotImplementedException("Subscription cancellation not implemented yet");
    }

    public async Task<SubscriptionDto> ReactivateSubscriptionAsync(Guid userId)
    {
        throw new NotImplementedException("Subscription reactivation not implemented yet");
    }

    public async Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync()
    {
        // Return hardcoded plans - no need for database storage
        return new List<SubscriptionPlanDto>
        {
            GetHardcodedPlan(SubscriptionPlan.Free),
            GetHardcodedPlan(SubscriptionPlan.Pro),
            GetHardcodedPlan(SubscriptionPlan.Enterprise)
        };
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        return subscription?.Status == SubscriptionStatus.Active;
    }

    public async Task<bool> CanAccessFeatureAsync(Guid userId, string featureName)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            return false;
        }

        // For now, all features are available for active subscriptions
        // This should be enhanced to check plan features based on PlanId
        return featureName.ToUpperInvariant() switch
        {
            "ALERTS" => false, // Only for paid plans
            "API" => false,   // Only for paid plans
            "PRIORITY_SUPPORT" => false, // Only for enterprise
            _ => true // Basic features available for free
        };
    }

    public async Task<int> GetMaxServersForUserAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            return 1; // Default to 1 server for free/unsubscribed users
        }

        // For now, return 1 for all plans
        // This should be enhanced to check plan features based on PlanId
        return 1;
    }

    public async Task CreateFreeSubscriptionAsync(Guid userId)
    {
        this.logger.LogInformation("Creating free subscription for user {UserId}", userId);

        // Check if user already has a subscription
        var existingSubscription = await this.subscriptionRepository.GetByUserIdAsync(userId);
        if (existingSubscription != null)
        {
            this.logger.LogInformation("User {UserId} already has subscription {SubscriptionId}", userId, existingSubscription.Id);
            return;
        }

        // Get free plan ID (hardcoded for now - should be configurable)
        var freePlanId = new Guid("841bb3db-424c-46e5-a752-04641391c993");

        // Create free subscription
        var freeSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = freePlanId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddYears(100), // Never expires for free plan
            CancelAtPeriodEnd = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await this.subscriptionRepository.AddAsync(freeSubscription);
        this.logger.LogInformation("Created free subscription {SubscriptionId} for user {UserId}", freeSubscription.Id, userId);
    }

    private static SubscriptionPlanDto GetHardcodedPlan(SubscriptionPlan planType)
    {
        return planType switch
        {
            SubscriptionPlan.Free => new SubscriptionPlanDto
            {
                Id = Guid.NewGuid(),
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
                Features = new List<string> { "1 server monitoring", "7 days retention" }
            },
            SubscriptionPlan.Pro => new SubscriptionPlanDto
            {
                Id = Guid.NewGuid(),
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
                Features = new List<string> { "10 servers", "30 days retention", "Real-time alerts", "API access" }
            },
            SubscriptionPlan.Enterprise => new SubscriptionPlanDto
            {
                Id = Guid.NewGuid(),
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
                Features = new List<string> { "Unlimited servers", "90 days retention", "Advanced alerts", "Full API access", "Priority support" }
            },
            _ => throw new InvalidOperationException($"Unknown plan type: {planType}")
        };
    }
}
