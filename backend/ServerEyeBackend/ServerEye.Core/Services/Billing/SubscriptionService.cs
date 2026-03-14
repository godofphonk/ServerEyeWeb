namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

public class SubscriptionService(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    IPaymentProviderFactory providerFactory,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    public async Task<Subscription?> GetUserSubscriptionAsync(Guid userId)
    {
        return await subscriptionRepository.GetByUserIdAsync(userId);
    }

    public async Task<Subscription> CreateSubscriptionAsync(Guid userId, Guid planId, string? paymentMethodId = null)
    {
        var plan = await planRepository.GetByIdAsync(planId) 
            ?? throw new InvalidOperationException("Plan not found");

        var existingSubscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (existingSubscription != null && existingSubscription.Status == SubscriptionStatus.Active)
        {
            throw new InvalidOperationException("User already has an active subscription");
        }

        var provider = providerFactory.GetProvider(PaymentProvider.Stripe);
        
        var customerId = existingSubscription?.ProviderCustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            var customerResult = await provider.CreateCustomerAsync(userId, $"user_{userId}@servereye.dev");
            if (!customerResult.Success || string.IsNullOrEmpty(customerResult.CustomerId))
            {
                throw new InvalidOperationException($"Failed to create customer: {customerResult.Error}");
            }
            customerId = customerResult.CustomerId;
        }

        var subscriptionRequest = new CreateSubscriptionRequest(
            customerId,
            planId,
            paymentMethodId
        );

        var result = await provider.CreateSubscriptionAsync(subscriptionRequest);
        if (!result.Success || string.IsNullOrEmpty(result.SubscriptionId))
        {
            throw new InvalidOperationException($"Failed to create subscription: {result.Error}");
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            Status = result.Status ?? SubscriptionStatus.Incomplete,
            Provider = PaymentProvider.Stripe,
            ProviderCustomerId = customerId,
            ProviderSubscriptionId = result.SubscriptionId,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (existingSubscription != null)
        {
            subscription.Id = existingSubscription.Id;
            await subscriptionRepository.UpdateAsync(subscription);
        }
        else
        {
            await subscriptionRepository.AddAsync(subscription);
        }

        logger.LogInformation("Created subscription {SubscriptionId} for user {UserId} with plan {PlanId}", 
            subscription.Id, userId, planId);

        return subscription;
    }

    public async Task<Subscription> UpdateSubscriptionAsync(Guid userId, Guid newPlanId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId) 
            ?? throw new InvalidOperationException("No active subscription found");

        if (subscription.PlanId == newPlanId)
        {
            throw new InvalidOperationException("User is already on this plan");
        }

        var newPlan = await planRepository.GetByIdAsync(newPlanId) 
            ?? throw new InvalidOperationException("Plan not found");

        var provider = providerFactory.GetProvider(subscription.Provider);
        
        if (string.IsNullOrEmpty(subscription.ProviderSubscriptionId))
        {
            throw new InvalidOperationException("Provider subscription ID not found");
        }

        var updateRequest = new UpdateSubscriptionRequest(newPlanId, ProrationBehavior: true);
        var result = await provider.UpdateSubscriptionAsync(subscription.ProviderSubscriptionId, updateRequest);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to update subscription: {result.Error}");
        }

        subscription.PlanId = newPlanId;
        subscription.UpdatedAt = DateTime.UtcNow;

        await subscriptionRepository.UpdateAsync(subscription);

        logger.LogInformation("Updated subscription {SubscriptionId} to plan {PlanId}", 
            subscription.Id, newPlanId);

        return subscription;
    }

    public async Task CancelSubscriptionAsync(Guid userId, bool immediately = false)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId) 
            ?? throw new InvalidOperationException("No active subscription found");

        var provider = providerFactory.GetProvider(subscription.Provider);
        
        if (string.IsNullOrEmpty(subscription.ProviderSubscriptionId))
        {
            throw new InvalidOperationException("Provider subscription ID not found");
        }

        var result = await provider.CancelSubscriptionAsync(subscription.ProviderSubscriptionId, immediately);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to cancel subscription: {result.Error}");
        }

        subscription.Status = immediately ? SubscriptionStatus.Canceled : subscription.Status;
        subscription.CanceledAt = result.CanceledAt;
        subscription.AutoRenew = false;
        subscription.UpdatedAt = DateTime.UtcNow;

        await subscriptionRepository.UpdateAsync(subscription);

        logger.LogInformation("Canceled subscription {SubscriptionId}, immediately: {Immediately}", 
            subscription.Id, immediately);
    }

    public async Task ResumeSubscriptionAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId) 
            ?? throw new InvalidOperationException("No subscription found");

        if (subscription.Status != SubscriptionStatus.Canceled || subscription.CanceledAt == null)
        {
            throw new InvalidOperationException("Subscription is not canceled");
        }

        subscription.CanceledAt = null;
        subscription.AutoRenew = true;
        subscription.UpdatedAt = DateTime.UtcNow;

        await subscriptionRepository.UpdateAsync(subscription);

        logger.LogInformation("Resumed subscription {SubscriptionId}", subscription.Id);
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        return subscription != null && 
               (subscription.Status == SubscriptionStatus.Active || 
                subscription.Status == SubscriptionStatus.Trialing);
    }

    public async Task<bool> CanAccessFeatureAsync(Guid userId, string feature)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            return false;
        }

        var plan = await planRepository.GetByIdAsync(subscription.PlanId);
        if (plan == null)
        {
            return false;
        }

        return feature switch
        {
            "alerts" => plan.HasAlerts,
            "api_access" => plan.HasApiAccess,
            "priority_support" => plan.HasPrioritySupport,
            _ => false
        };
    }

    public async Task<SubscriptionLimits> GetSubscriptionLimitsAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            return new SubscriptionLimits(
                MaxServers: 1,
                MetricsRetentionDays: 7,
                HasAlerts: false,
                HasApiAccess: false,
                HasPrioritySupport: false
            );
        }

        var plan = await planRepository.GetByIdAsync(subscription.PlanId);
        if (plan == null)
        {
            return new SubscriptionLimits(
                MaxServers: 1,
                MetricsRetentionDays: 7,
                HasAlerts: false,
                HasApiAccess: false,
                HasPrioritySupport: false
            );
        }

        return new SubscriptionLimits(
            MaxServers: plan.MaxServers,
            MetricsRetentionDays: plan.MetricsRetentionDays,
            HasAlerts: plan.HasAlerts,
            HasApiAccess: plan.HasApiAccess,
            HasPrioritySupport: plan.HasPrioritySupport
        );
    }

    public async Task HandleSubscriptionEventAsync(SubscriptionEventDto eventDto)
    {
        var subscription = await subscriptionRepository.GetByProviderSubscriptionIdAsync(eventDto.SubscriptionId);
        if (subscription == null)
        {
            logger.LogWarning("Subscription not found for provider ID {ProviderSubscriptionId}", 
                eventDto.SubscriptionId);
            return;
        }

        subscription.Status = eventDto.Status;
        subscription.UpdatedAt = DateTime.UtcNow;

        if (eventDto.CurrentPeriodEnd.HasValue)
        {
            subscription.CurrentPeriodEnd = eventDto.CurrentPeriodEnd.Value;
        }

        if (eventDto.CanceledAt.HasValue)
        {
            subscription.CanceledAt = eventDto.CanceledAt.Value;
        }

        await subscriptionRepository.UpdateAsync(subscription);

        logger.LogInformation("Handled subscription event {EventType} for subscription {SubscriptionId}", 
            eventDto.EventType, subscription.Id);
    }
}
