using System.Diagnostics;
namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.Configuration.Plans;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Interfaces.Services.Billing;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly IPaymentService paymentService;
    private readonly ILogger<SubscriptionService> logger;
    private readonly IMetricsCacheService cacheService;
    private readonly CacheSettings cacheSettings;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IPaymentService paymentService,
        ILogger<SubscriptionService> logger,
        IMetricsCacheService cacheService,
        CacheSettings cacheSettings)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.paymentService = paymentService;
        this.logger = logger;
        this.cacheService = cacheService;
        this.cacheSettings = cacheSettings;
    }

    public async Task<SubscriptionDto?> GetUserSubscriptionAsync(Guid userId)
    {
        this.logger.LogInformation("Getting subscription for user: {UserId}", userId);

        var cacheKey = $"subscription:{userId}";
        var cachedResult = await this.cacheService.GetAsync<SubscriptionDto>(cacheKey);

        if (cachedResult != null)
        {
            this.logger.LogDebug("Cache hit for user subscription: {UserId}", userId);
            return cachedResult;
        }

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null)
        {
            this.logger.LogWarning("No subscription found for user: {UserId}", userId);
            return null;
        }

        this.logger.LogInformation(
            "Found subscription: {SubscriptionId}, PlanId: {PlanId}, Status: {Status}",
            subscription.Id,
            subscription.PlanId,
            subscription.Status);

        // Get plan definition from code
        var planDefinition = SubscriptionPlanDefinitions.GetById(subscription.PlanId);
        if (planDefinition == null)
        {
            this.logger.LogWarning("Plan definition not found for PlanId: {PlanId}", subscription.PlanId);
            planDefinition = SubscriptionPlanDefinitions.Free; // Fallback to Free plan
        }

        var result = new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            PlanType = planDefinition.PlanType,
            PlanName = planDefinition.Name,
            Status = subscription.Status,
            Amount = planDefinition.MonthlyPrice,
            Currency = "usd",
            IsYearly = false,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CanceledAt = null,
            TrialEnd = null,
            CreatedAt = subscription.CreatedAt
        };

        await this.cacheService.SetAsync(cacheKey, result, this.cacheSettings.UserSubscription);

        return result;
    }

    public async Task<CreateSubscriptionResponse> CreateSubscriptionCheckoutAsync(
        Guid userId,
        CreateSubscriptionRequest request)
    {
        this.logger.LogInformation("Creating subscription checkout for user: {UserId}, plan: {PlanType}, yearly: {IsYearly}", userId, request.PlanType, request.IsYearly);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await paymentService.CreateSubscriptionCheckoutAsync(userId, request);

            stopwatch.Stop();

            this.logger.LogInformation("Subscription checkout created successfully for user: {UserId}, sessionId: {SessionId} in {ElapsedMs}ms", userId, result.SessionId, stopwatch.ElapsedMilliseconds);

            // Business metric: MRR impact tracking
            var plan = SubscriptionPlanDefinitions.GetByType(request.PlanType);
            if (plan == null)
            {
                this.logger.LogWarning("Plan definition not found for PlanType: {PlanType}", request.PlanType);
                plan = SubscriptionPlanDefinitions.Free;
            }
            var monthlyRevenue = request.IsYearly ? plan.YearlyPrice / 12 : plan.MonthlyPrice;

            this.logger.LogInformation(
                "MRR impact: Potential +{MonthlyRevenue:F2} USD from user {UserId}, plan {PlanType} ({BillingCycle})",
                monthlyRevenue,
                userId,
                request.PlanType,
                request.IsYearly ? "yearly" : "monthly");

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            this.logger.LogError(ex, "Failed to create subscription checkout for user: {UserId} in {ElapsedMs}ms: {ErrorType}", userId, stopwatch.ElapsedMilliseconds, ex.GetType().Name);

            // Business metric: Lost revenue opportunity
            var plan = SubscriptionPlanDefinitions.GetByType(request.PlanType);
            plan ??= SubscriptionPlanDefinitions.Free;
            this.logger.LogWarning(
                "Revenue opportunity lost: {PlanType} for user {UserId}, reason {ErrorType}",
                request.PlanType,
                userId,
                ex.GetType().Name);

            throw;
        }
    }

    public async Task<SubscriptionDto> UpdateSubscriptionPlanAsync(
        Guid userId,
        UpdateSubscriptionRequest request)
    {
        this.logger.LogInformation("Attempting to update subscription for user: {UserId}, new plan: {NewPlan}", userId, request.NewPlanType);
        throw new NotImplementedException("Subscription updates not implemented yet");
    }

    public async Task CancelSubscriptionAsync(
        Guid userId,
        CancelSubscriptionRequest request)
    {
        this.logger.LogWarning("Subscription cancellation requested for user: {UserId}, reason: {Reason}", userId, (request.CancellationReason ?? "Not specified").Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));
        throw new NotImplementedException("Subscription cancellation not implemented yet");
    }

    public async Task<SubscriptionDto> ReactivateSubscriptionAsync(Guid userId)
    {
        throw new NotImplementedException("Subscription reactivation not implemented yet");
    }

    public async Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync()
    {
        // Return plans from code definitions
        var definitions = SubscriptionPlanDefinitions.GetAll();
        return definitions.Select(MapToDto).ToList();
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
            this.logger.LogWarning("Feature access denied for user: {UserId}, feature: {Feature} - no active subscription", userId, featureName);
            return false;
        }

        var planDefinition = SubscriptionPlanDefinitions.GetById(subscription.PlanId);
        if (planDefinition == null)
        {
            this.logger.LogWarning("Plan definition not found for PlanId: {PlanId}, denying feature access", subscription.PlanId);
            return false;
        }

        return featureName.ToUpperInvariant() switch
        {
            "ALERTS" => planDefinition.HasAlerts,
            "API" => planDefinition.HasApiAccess,
            "PRIORITY_SUPPORT" => planDefinition.HasPrioritySupport,
            _ => true // Basic features available for all plans
        };
    }

    public async Task<int> GetMaxServersForUserAsync(Guid userId)
    {
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            return SubscriptionPlanDefinitions.Free.MaxServers; // Default to Free plan limit
        }

        var planDefinition = SubscriptionPlanDefinitions.GetById(subscription.PlanId);
        if (planDefinition == null)
        {
            this.logger.LogWarning("Plan definition not found for PlanId: {PlanId}, using Free plan limit", subscription.PlanId);
            return SubscriptionPlanDefinitions.Free.MaxServers;
        }

        return planDefinition.MaxServers;
    }

    public async Task CreateFreeSubscriptionAsync(Guid userId)
    {
        this.logger.LogInformation("Creating free subscription for user {UserId}", userId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check if user already has a subscription
            var existingSubscription = await this.subscriptionRepository.GetByUserIdAsync(userId);
            if (existingSubscription != null)
            {
                this.logger.LogInformation("User {UserId} already has subscription {SubscriptionId}", userId, existingSubscription.Id);
                return;
            }

            var freeSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = SubscriptionPlanDefinitions.Free.Id,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = null, // No end date for free plan
                CreatedAt = DateTime.UtcNow
            };

            await this.subscriptionRepository.AddAsync(freeSubscription);

            stopwatch.Stop();

            this.logger.LogInformation("Created free subscription {SubscriptionId} for user {UserId} in {ElapsedMs}ms", freeSubscription.Id, userId, stopwatch.ElapsedMilliseconds);

            // Business metric: User onboarding
            this.logger.LogInformation(
                "User onboarding: {UserId} started with free plan {SubscriptionId}",
                userId,
                freeSubscription.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            this.logger.LogError(ex, "Failed to create free subscription for user {UserId} in {ElapsedMs}ms: {ErrorType}", userId, stopwatch.ElapsedMilliseconds, ex.GetType().Name);

            // Business metric: Onboarding failure
            this.logger.LogWarning(
                "User onboarding failed: {UserId}, reason {ErrorType}",
                userId,
                ex.GetType().Name);

            throw;
        }
    }

    private static SubscriptionPlanDto MapToDto(SubscriptionPlanDefinition definition)
    {
        return new SubscriptionPlanDto
        {
            Id = definition.Id,
            PlanType = definition.PlanType,
            Name = definition.Name,
            Description = definition.Description,
            MonthlyPrice = definition.MonthlyPrice,
            YearlyPrice = definition.YearlyPrice,
            MaxServers = definition.MaxServers,
            MetricsRetentionDays = definition.MetricsRetentionDays,
            HasAlerts = definition.HasAlerts,
            HasApiAccess = definition.HasApiAccess,
            HasPrioritySupport = definition.HasPrioritySupport,
            Features = definition.Features
        };
    }
}
