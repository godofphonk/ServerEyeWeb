namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration.Plans;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Interfaces.Services.Billing;

/// <summary>
/// Service for checking and retrieving subscription plan limits for users.
/// </summary>
public class PlanLimitsService(
    ISubscriptionRepository subscriptionRepository,
    IUserServerAccessRepository userServerAccessRepository,
    ILogger<PlanLimitsService> logger) : IPlanLimitsService
{
    private readonly ISubscriptionRepository subscriptionRepository = subscriptionRepository;
    private readonly IUserServerAccessRepository userServerAccessRepository = userServerAccessRepository;
    private readonly ILogger<PlanLimitsService> logger = logger;

    public async Task<UserPlanLimitsDto> GetUserLimitsAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        SubscriptionPlanDefinition? planDefinition;

        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            // Default to Free plan for users without active subscription
            planDefinition = SubscriptionPlanDefinitions.Free;
            logger.LogInformation("User {UserId} has no active subscription, using Free plan limits", userId);
        }
        else
        {
            planDefinition = SubscriptionPlanDefinitions.GetById(subscription.PlanId);
            if (planDefinition == null)
            {
                logger.LogWarning("Plan definition not found for PlanId: {PlanId}, using Free plan limits", subscription.PlanId);
                planDefinition = SubscriptionPlanDefinitions.Free;
            }
        }

        var servers = await userServerAccessRepository.GetUserServersAsync(userId);
        var currentServers = servers.Count;

        return new UserPlanLimitsDto
        {
            MaxServers = planDefinition.MaxServers,
            CurrentServers = currentServers,
            MetricsRetentionDays = planDefinition.MetricsRetentionDays,
            PlanType = planDefinition.PlanType,
            PlanName = planDefinition.Name,
            HasActiveSubscription = subscription?.Status == SubscriptionStatus.Active
        };
    }

    public async Task<bool> CanAddServerAsync(Guid userId)
    {
        var limits = await GetUserLimitsAsync(userId);

        // -1 indicates unlimited (Enterprise)
        if (limits.MaxServers == -1)
        {
            return true;
        }

        return limits.CurrentServers < limits.MaxServers;
    }

    public async Task<int> GetMetricsRetentionDaysAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        SubscriptionPlanDefinition? planDefinition;

        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            planDefinition = SubscriptionPlanDefinitions.Free;
        }
        else
        {
            planDefinition = SubscriptionPlanDefinitions.GetById(subscription.PlanId);
            if (planDefinition == null)
            {
                logger.LogWarning("Plan definition not found for PlanId: {PlanId}, using Free plan retention", subscription.PlanId);
                planDefinition = SubscriptionPlanDefinitions.Free;
            }
        }

        return planDefinition.MetricsRetentionDays;
    }
}
