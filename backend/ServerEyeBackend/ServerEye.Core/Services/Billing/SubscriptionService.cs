using System.Diagnostics;
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
    private readonly IPaymentService paymentService;
    private readonly ILogger<SubscriptionService> logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IPaymentService paymentService,
        ILogger<SubscriptionService> logger)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.paymentService = paymentService;
        this.logger = logger;
    }

    public async Task<SubscriptionDto?> GetUserSubscriptionAsync(Guid userId)
    {
        this.logger.LogInformation("Getting subscription for user: {UserId}", userId);

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

        // Map plan based on PlanId
        SubscriptionPlan planType;
        string planName;

        switch (subscription.PlanId.ToString())
        {
            case var id when id == "f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c": // Free plan ID
                planType = SubscriptionPlan.Free;
                planName = "Free";
                break;
            case var id when id == "841bb3db-424c-46e5-a752-04641391c993": // Pro plan ID
                planType = SubscriptionPlan.Pro;
                planName = "Pro";
                break;
            case var id when id == "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d": // Enterprise plan ID
                planType = SubscriptionPlan.Enterprise;
                planName = "Enterprise";
                break;
            default:
                planType = SubscriptionPlan.Free;
                planName = "Free";
                break;
        }

        return new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            PlanType = planType,
            PlanName = planName,
            Status = subscription.Status,
            Amount = planType == SubscriptionPlan.Free ? 0 : 9.99m, // Default Pro price
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
        this.logger.LogInformation("Creating subscription checkout for user: {UserId}, plan: {PlanType}, yearly: {IsYearly}", userId, request.PlanType, request.IsYearly);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await paymentService.CreateSubscriptionCheckoutAsync(userId, request);

            stopwatch.Stop();

            this.logger.LogInformation("Subscription checkout created successfully for user: {UserId}, sessionId: {SessionId} in {ElapsedMs}ms", userId, result.SessionId, stopwatch.ElapsedMilliseconds);

            // Business metric: MRR impact tracking
            var plan = GetHardcodedPlan(request.PlanType);
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
            var plan = GetHardcodedPlan(request.PlanType);
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
            this.logger.LogWarning("Feature access denied for user: {UserId}, feature: {Feature} - no active subscription", userId, featureName);
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
                PlanId = Guid.Parse("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c"), // Free plan ID from subscriptionplans table
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
