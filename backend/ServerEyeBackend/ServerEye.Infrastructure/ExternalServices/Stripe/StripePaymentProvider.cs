namespace ServerEye.Infrastructure.ExternalServices.Stripe;

using global::Stripe;
using global::Stripe.Checkout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services.Billing;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly StripeConfiguration config;
    private readonly ILogger<StripePaymentProvider> logger;

    public StripePaymentProvider(
        IOptions<StripeConfiguration> config,
        ILogger<StripePaymentProvider> logger)
    {
        this.config = config.Value;
        this.logger = logger;

        // Log the actual key being used (first 8 chars for security)
        var keyPrefix = this.config.SecretKey?.Length >= 8
            ? this.config.SecretKey[..8]
            : "INVALID";
        logger.LogInformation("Initializing Stripe with key prefix: {KeyPrefix}...", keyPrefix);
        logger.LogInformation("Full key length: {KeyLength}", this.config.SecretKey?.Length ?? 0);

        global::Stripe.StripeConfiguration.ApiKey = this.config.SecretKey;

        // Verify it was set correctly
        var setKeyPrefix = global::Stripe.StripeConfiguration.ApiKey?.Length >= 8
            ? global::Stripe.StripeConfiguration.ApiKey[..8]
            : "INVALID";
        logger.LogInformation("StripeConfiguration.ApiKey set to: {SetKeyPrefix}", setKeyPrefix);
    }

    public PaymentProvider ProviderType => PaymentProvider.Stripe;

    public async Task<string> CreateCustomerAsync(Guid userId, string email, string? name = null)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() }
                }
            };

            var service = new CustomerService();

            // Log the API key being used for this specific call
            var currentKey = global::Stripe.StripeConfiguration.ApiKey?.Length >= 8
                ? global::Stripe.StripeConfiguration.ApiKey[..8]
                : "INVALID";
            logger.LogInformation("Making Stripe API call with key prefix: {KeyPrefix}", currentKey);

            var customer = await service.CreateAsync(options);

            logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);

            // Business metric: Customer acquisition
            logger.LogInformation(
                "Customer acquisition: New Stripe customer {CustomerId} created for user {UserId}",
                customer.Id,
                userId);

            return customer.Id;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create Stripe customer for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CreateSubscriptionResponse> CreateCheckoutSessionAsync(
        string customerId,
        Guid userId,
        SubscriptionPlan planType,
        bool isYearly,
        string successUrl,
        string cancelUrl)
    {
        try
        {
            var priceId = GetPriceId(planType, isYearly);

            var options = new SessionCreateOptions
            {
                Customer = customerId,
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                AllowPromotionCodes = true,
                BillingAddressCollection = "auto",
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() },
                    { "plan_type", planType.ToString() },
                    { "is_yearly", isYearly.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            logger.LogInformation(
                "Created Stripe checkout session {SessionId} for customer {CustomerId}",
                session.Id,
                customerId);

            // Business metric: Checkout session creation
            logger.LogInformation(
                "Checkout funnel: Stripe session {SessionId} created for {PlanType} ({BillingCycle})",
                session.Id,
                planType,
                isYearly ? "yearly" : "monthly");

            return new CreateSubscriptionResponse
            {
                SessionId = session.Id,
                SessionUrl = session.Url
            };
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create Stripe checkout session for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<string> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        DateTime? trialEnd = null)
    {
        try
        {
            var options = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions { Price = priceId }
                },
                TrialEnd = trialEnd.HasValue ? trialEnd.Value : null,
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                }
            };

            var service = new SubscriptionService();
            var subscription = await service.CreateAsync(options);

            logger.LogInformation(
                "Created Stripe subscription {SubscriptionId} for customer {CustomerId}",
                subscription.Id,
                customerId);

            return subscription.Id;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create Stripe subscription for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task UpdateSubscriptionAsync(string subscriptionId, string newPriceId)
    {
        try
        {
            var service = new SubscriptionService();
            var subscription = await service.GetAsync(subscriptionId);

            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = newPriceId
                    }
                },
                ProrationBehavior = "create_prorations"
            };

            await service.UpdateAsync(subscriptionId, options);

            logger.LogInformation(
                "Updated Stripe subscription {SubscriptionId} to price {PriceId}",
                subscription.Id,
                newPriceId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to update Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately)
    {
        try
        {
            var service = new SubscriptionService();

            if (cancelImmediately)
            {
                await service.CancelAsync(subscriptionId);
                logger.LogInformation(
                "Canceled Stripe subscription {SubscriptionId}, immediate: {Immediate}",
                subscriptionId,
                cancelImmediately);
            }
            else
            {
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                };
                await service.UpdateAsync(subscriptionId, options);
                logger.LogInformation(
                    "Scheduled Stripe subscription {SubscriptionId} for cancellation at period end",
                    subscriptionId);
            }
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to cancel Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(
        string customerId,
        decimal amount,
        string currency,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency,
                Customer = customerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            logger.LogInformation(
                "Created Stripe payment intent {PaymentIntentId} for customer {CustomerId}",
                paymentIntent.Id,
                customerId);

            return new CreatePaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id
            };
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create Stripe payment intent for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<bool> RefundPaymentAsync(string paymentId, decimal? amount = null)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100);
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            logger.LogInformation(
                "Created Stripe refund {RefundId} for payment {PaymentId}",
                refund.Id,
                paymentId);

            return refund.Status == "succeeded";
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to refund Stripe payment {PaymentId}", paymentId);
            return false;
        }
    }

    public async Task<object> GetSubscriptionDetailsAsync(string subscriptionId)
    {
        try
        {
            var service = new SubscriptionService();
            return await service.GetAsync(subscriptionId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to get Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<object> GetPaymentDetailsAsync(string paymentId)
    {
        try
        {
            var service = new PaymentIntentService();
            return await service.GetAsync(paymentId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to get Stripe payment {PaymentId}", paymentId);
            throw;
        }
    }

    public Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, string secret)
    {
        try
        {
            EventUtility.ConstructEvent(payload, signature, secret, throwOnApiVersionMismatch: false);
            return Task.FromResult(true);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Failed to verify Stripe webhook signature");
            return Task.FromResult(false);
        }
    }

    public Task<(string EventType, object Data)> ParseWebhookEventAsync(string payload)
    {
        try
        {
            var stripeEvent = EventUtility.ParseEvent(payload, throwOnApiVersionMismatch: false);
            return Task.FromResult((stripeEvent.Type, (object)stripeEvent.Data.Object));
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to parse Stripe webhook event");
            throw;
        }
    }

    private string GetPriceId(SubscriptionPlan planType, bool isYearly)
    {
        var key = $"{planType}_{(isYearly ? "Yearly" : "Monthly")}";

        if (!config.PriceIds.TryGetValue(key, out var priceId))
        {
            throw new InvalidOperationException($"Price ID not configured for {key}");
        }

        return priceId;
    }
}
