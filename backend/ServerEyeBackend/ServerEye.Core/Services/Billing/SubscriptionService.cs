namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly ISubscriptionPlanRepository planRepository;
    private readonly IUserRepository userRepository;
    private readonly IPaymentProviderFactory providerFactory;
    private readonly ILogger<SubscriptionService> logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IUserRepository userRepository,
        IPaymentProviderFactory providerFactory,
        ILogger<SubscriptionService> logger)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.planRepository = planRepository;
        this.userRepository = userRepository;
        this.providerFactory = providerFactory;
        this.logger = logger;
    }

    public async Task<SubscriptionDto?> GetUserSubscriptionAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null)
        {
            return null;
        }

        var plan = await planRepository.GetByPlanTypeAsync(subscription.PlanType);

        return new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            PlanType = subscription.PlanType,
            PlanName = plan?.Name ?? subscription.PlanType.ToString(),
            Status = subscription.Status,
            Amount = subscription.Amount,
            Currency = subscription.Currency,
            IsYearly = subscription.IsYearly,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CanceledAt = subscription.CanceledAt,
            TrialEnd = subscription.TrialEnd,
            CreatedAt = subscription.CreatedAt
        };
    }

    public async Task<CreateSubscriptionResponse> CreateSubscriptionCheckoutAsync(
        Guid userId,
        CreateSubscriptionRequest request)
    {
        logger.LogInformation(
            "Creating subscription checkout for user {UserId}, plan {PlanType}",
            userId,
            request.PlanType);

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var existingSubscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (existingSubscription != null && existingSubscription.Status == SubscriptionStatus.Active)
        {
            throw new InvalidOperationException("User already has an active subscription");
        }

        var plan = await planRepository.GetByPlanTypeAsync(request.PlanType)
            ?? throw new InvalidOperationException($"Plan {request.PlanType} not found");

        var provider = providerFactory.GetDefaultProvider();

        string customerId;
        if (existingSubscription?.ProviderCustomerId != null)
        {
            customerId = existingSubscription.ProviderCustomerId;
        }
        else
        {
            customerId = await provider.CreateCustomerAsync(userId, user.Email ?? string.Empty, user.UserName);
        }

        var successUrl = request.SuccessUrl ?? "http://localhost:3000/dashboard?subscription=success";
        var cancelUrl = request.CancelUrl ?? "http://localhost:3000/pricing?subscription=canceled";

        var checkoutResponse = await provider.CreateCheckoutSessionAsync(
            customerId,
            request.PlanType,
            request.IsYearly,
            successUrl,
            cancelUrl);

        if (existingSubscription == null)
        {
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanType = request.PlanType,
                Status = SubscriptionStatus.Incomplete,
                Provider = provider.ProviderType,
                ProviderCustomerId = customerId,
                Amount = request.IsYearly ? plan.YearlyPrice : plan.MonthlyPrice,
                Currency = "usd",
                IsYearly = request.IsYearly,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await subscriptionRepository.AddAsync(subscription);
        }

        logger.LogInformation(
            "Created checkout session {SessionId} for user {UserId}",
            checkoutResponse.SessionId,
            userId);

        return checkoutResponse;
    }

    public async Task<SubscriptionDto> UpdateSubscriptionPlanAsync(
        Guid userId,
        UpdateSubscriptionRequest request)
    {
        logger.LogInformation(
            "Updating subscription for user {UserId} to plan {PlanType}",
            userId,
            request.NewPlanType);

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("No subscription found");

        if (subscription.Status != SubscriptionStatus.Active)
        {
            throw new InvalidOperationException("Subscription is not active");
        }

        if (subscription.PlanType == request.NewPlanType && subscription.IsYearly == request.IsYearly)
        {
            throw new InvalidOperationException("Already on this plan");
        }

        var newPlan = await planRepository.GetByPlanTypeAsync(request.NewPlanType)
            ?? throw new InvalidOperationException($"Plan {request.NewPlanType} not found");

        var provider = providerFactory.GetProvider(subscription.Provider);

        var priceId = GetPriceIdForPlan(request.NewPlanType, request.IsYearly);
        await provider.UpdateSubscriptionAsync(subscription.ProviderSubscriptionId!, priceId);

        subscription.PlanType = request.NewPlanType;
        subscription.IsYearly = request.IsYearly;
        subscription.Amount = request.IsYearly ? newPlan.YearlyPrice : newPlan.MonthlyPrice;
        subscription.UpdatedAt = DateTime.UtcNow;

        await subscriptionRepository.UpdateAsync(subscription);

        logger.LogInformation(
            "Updated subscription {SubscriptionId} to plan {PlanType}",
            subscription.Id,
            request.NewPlanType);

        return await GetUserSubscriptionAsync(userId)
            ?? throw new InvalidOperationException("Failed to retrieve updated subscription");
    }

    public async Task CancelSubscriptionAsync(
        Guid userId,
        CancelSubscriptionRequest request)
    {
        logger.LogInformation("Canceling subscription for user {UserId}", userId);

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("No subscription found");

        if (subscription.Status == SubscriptionStatus.Canceled)
        {
            throw new InvalidOperationException("Subscription is already canceled");
        }

        var provider = providerFactory.GetProvider(subscription.Provider);

        await provider.CancelSubscriptionAsync(
            subscription.ProviderSubscriptionId!,
            request.CancelImmediately);

        if (request.CancelImmediately)
        {
            subscription.Status = SubscriptionStatus.Canceled;
        }

        subscription.CanceledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await subscriptionRepository.UpdateAsync(subscription);

        logger.LogInformation(
            "Canceled subscription {SubscriptionId} for user {UserId}",
            subscription.Id,
            userId);
    }

    public async Task<SubscriptionDto> ReactivateSubscriptionAsync(Guid userId)
    {
        logger.LogInformation("Reactivating subscription for user {UserId}", userId);

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("No subscription found");

        if (subscription.Status == SubscriptionStatus.Active)
        {
            throw new InvalidOperationException("Subscription is already active");
        }

        subscription.Status = SubscriptionStatus.Active;
        subscription.CanceledAt = null;
        subscription.UpdatedAt = DateTime.UtcNow;

        await subscriptionRepository.UpdateAsync(subscription);

        logger.LogInformation(
            "Reactivated subscription {SubscriptionId} for user {UserId}",
            subscription.Id,
            userId);

        return await GetUserSubscriptionAsync(userId)
            ?? throw new InvalidOperationException("Failed to retrieve reactivated subscription");
    }

    public async Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync()
    {
        // Return hardcoded plans - no need for database storage
        var plans = new List<SubscriptionPlanDto>
        {
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                PlanType = SubscriptionPlan.Free,
                Name = "Free",
                Description = "Perfect for getting started with server monitoring",
                MonthlyPrice = 0m,
                YearlyPrice = 0m,
                MaxServers = 3,
                MetricsRetentionDays = 7,
                HasAlerts = true,
                HasApiAccess = false,
                HasPrioritySupport = false,
                Features = new List<string> { "maxAlerts: 10", "webhooks: false" }
            },
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                PlanType = SubscriptionPlan.Pro,
                Name = "Pro",
                Description = "Advanced features for professional teams",
                MonthlyPrice = 9.99m,
                YearlyPrice = 99.99m,
                MaxServers = 10,
                MetricsRetentionDays = 30,
                HasAlerts = true,
                HasApiAccess = true,
                HasPrioritySupport = false,
                Features = new List<string> { "maxAlerts: 100", "webhooks: false" }
            },
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                PlanType = SubscriptionPlan.Enterprise,
                Name = "Enterprise",
                Description = "Complete solution for large organizations",
                MonthlyPrice = 29.99m,
                YearlyPrice = 299.99m,
                MaxServers = 50,
                MetricsRetentionDays = 90,
                HasAlerts = true,
                HasApiAccess = true,
                HasPrioritySupport = true,
                Features = new List<string> { "maxAlerts: 1000", "webhooks: true" }
            }
        };

        return await Task.FromResult(plans);
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

        var plan = await planRepository.GetByPlanTypeAsync(subscription.PlanType);
        if (plan == null)
        {
            return false;
        }

        return featureName.ToUpperInvariant() switch
        {
            "ALERTS" => plan.HasAlerts,
            "API" => plan.HasApiAccess,
            "PRIORITY_SUPPORT" => plan.HasPrioritySupport,
            _ => false
        };
    }

    public async Task<int> GetMaxServersForUserAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            var freePlan = await planRepository.GetByPlanTypeAsync(SubscriptionPlan.Free);
            return freePlan?.MaxServers ?? 1;
        }

        var plan = await planRepository.GetByPlanTypeAsync(subscription.PlanType);
        return plan?.MaxServers ?? 1;
    }

    private static string GetPriceIdForPlan(SubscriptionPlan planType, bool isYearly)
    {
        return $"{planType}_{(isYearly ? "Yearly" : "Monthly")}";
    }
}
