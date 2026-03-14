namespace ServerEye.Infrastructure.ExternalServices.Stripe;

using global::Stripe;
using global::Stripe.Checkout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly StripeClient stripeClient;
    private readonly ILogger<StripePaymentProvider> logger;
    private readonly ISubscriptionPlanRepository planRepository;
    private readonly StripeOptions options;

    public StripePaymentProvider(
        IOptions<StripeOptions> options,
        ILogger<StripePaymentProvider> logger,
        ISubscriptionPlanRepository planRepository)
    {
        this.options = options.Value;
        this.logger = logger;
        this.planRepository = planRepository;
        
        this.stripeClient = new StripeClient(this.options.SecretKey);
    }

    public async Task<CreateCustomerResult> CreateCustomerAsync(Guid userId, string email, string? name = null)
    {
        try
        {
            var customerService = new CustomerService(stripeClient);
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() }
                }
            };

            var customer = await customerService.CreateAsync(options);
            
            logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);
            
            return new CreateCustomerResult(true, customer.Id);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create Stripe customer for user {UserId}", userId);
            return new CreateCustomerResult(false, null, ex.Message);
        }
    }

    public async Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
    {
        try
        {
            var paymentIntentService = new PaymentIntentService(stripeClient);
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100),
                Currency = request.Currency.ToLower(),
                Description = request.Description,
                Metadata = request.Metadata?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                ) ?? new Dictionary<string, string>()
            };

            options.Metadata["user_id"] = request.UserId.ToString();

            var paymentIntent = await paymentIntentService.CreateAsync(options);
            
            logger.LogInformation("Created payment intent {PaymentIntentId} for user {UserId}", 
                paymentIntent.Id, request.UserId);
            
            return new CreatePaymentIntentResult(
                true,
                paymentIntent.Id,
                paymentIntent.ClientSecret
            );
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create payment intent for user {UserId}", request.UserId);
            return new CreatePaymentIntentResult(false, null, null, ex.Message);
        }
    }

    public async Task<CreateSubscriptionResult> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        try
        {
            var plan = await planRepository.GetByIdAsync(request.PlanId);
            if (plan == null)
            {
                return new CreateSubscriptionResult(false, null, null, null, "Plan not found");
            }

            var priceId = plan.Metadata.TryGetValue("stripe_price_id", out var value) 
                ? value?.ToString() 
                : null;

            if (string.IsNullOrEmpty(priceId))
            {
                return new CreateSubscriptionResult(false, null, null, null, "Stripe price ID not configured for plan");
            }

            var subscriptionService = new SubscriptionService(stripeClient);
            var options = new SubscriptionCreateOptions
            {
                Customer = request.CustomerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions { Price = priceId }
                },
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                },
                Metadata = request.Metadata?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                ) ?? new Dictionary<string, string>()
            };

            if (request.PaymentMethodId != null)
            {
                options.DefaultPaymentMethod = request.PaymentMethodId;
            }

            if (request.TrialDays.HasValue && request.TrialDays.Value > 0)
            {
                options.TrialPeriodDays = request.TrialDays.Value;
            }

            options.Metadata["plan_id"] = request.PlanId.ToString();

            var subscription = await subscriptionService.CreateAsync(options);
            
            logger.LogInformation("Created Stripe subscription {SubscriptionId} for customer {CustomerId}", 
                subscription.Id, request.CustomerId);
            
            var status = MapStripeStatus(subscription.Status);
            
            return new CreateSubscriptionResult(
                true,
                subscription.Id,
                subscription.LatestInvoice?.PaymentIntent?.ClientSecret,
                status
            );
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create subscription for customer {CustomerId}", request.CustomerId);
            return new CreateSubscriptionResult(false, null, null, null, ex.Message);
        }
    }

    public async Task<CancelSubscriptionResult> CancelSubscriptionAsync(string subscriptionId, bool immediately = false)
    {
        try
        {
            var subscriptionService = new SubscriptionService(stripeClient);
            
            Subscription subscription;
            if (immediately)
            {
                subscription = await subscriptionService.CancelAsync(subscriptionId);
            }
            else
            {
                subscription = await subscriptionService.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                });
            }
            
            logger.LogInformation("Canceled Stripe subscription {SubscriptionId}, immediately: {Immediately}", 
                subscriptionId, immediately);
            
            return new CancelSubscriptionResult(
                true,
                subscription.Id,
                subscription.CanceledAt
            );
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to cancel subscription {SubscriptionId}", subscriptionId);
            return new CancelSubscriptionResult(false, null, null, ex.Message);
        }
    }

    public async Task<UpdateSubscriptionResult> UpdateSubscriptionAsync(string subscriptionId, UpdateSubscriptionRequest request)
    {
        try
        {
            var plan = await planRepository.GetByIdAsync(request.NewPlanId);
            if (plan == null)
            {
                return new UpdateSubscriptionResult(false, null, "Plan not found");
            }

            var priceId = plan.Metadata.TryGetValue("stripe_price_id", out var value) 
                ? value?.ToString() 
                : null;

            if (string.IsNullOrEmpty(priceId))
            {
                return new UpdateSubscriptionResult(false, null, "Stripe price ID not configured for plan");
            }

            var subscriptionService = new SubscriptionService(stripeClient);
            var subscription = await subscriptionService.GetAsync(subscriptionId);

            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = priceId
                    }
                },
                ProrationBehavior = request.ProrationBehavior ? "create_prorations" : "none"
            };

            var updatedSubscription = await subscriptionService.UpdateAsync(subscriptionId, options);
            
            logger.LogInformation("Updated Stripe subscription {SubscriptionId} to plan {PlanId}", 
                subscriptionId, request.NewPlanId);
            
            return new UpdateSubscriptionResult(true, updatedSubscription.Id);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to update subscription {SubscriptionId}", subscriptionId);
            return new UpdateSubscriptionResult(false, null, ex.Message);
        }
    }

    public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request)
    {
        try
        {
            var plan = await planRepository.GetByIdAsync(request.PlanId);
            if (plan == null)
            {
                return new CreateCheckoutSessionResult(false, null, null, "Plan not found");
            }

            var priceId = plan.Metadata.TryGetValue("stripe_price_id", out var value) 
                ? value?.ToString() 
                : null;

            if (string.IsNullOrEmpty(priceId))
            {
                return new CreateCheckoutSessionResult(false, null, null, "Stripe price ID not configured for plan");
            }

            var sessionService = new SessionService(stripeClient);
            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = request.Metadata?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                ) ?? new Dictionary<string, string>()
            };

            options.Metadata["user_id"] = request.UserId.ToString();
            options.Metadata["plan_id"] = request.PlanId.ToString();

            if (request.TrialDays.HasValue && request.TrialDays.Value > 0)
            {
                options.SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = request.TrialDays.Value
                };
            }

            var session = await sessionService.CreateAsync(options);
            
            logger.LogInformation("Created Stripe checkout session {SessionId} for user {UserId}", 
                session.Id, request.UserId);
            
            return new CreateCheckoutSessionResult(
                true,
                session.Id,
                session.Url
            );
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create checkout session for user {UserId}", request.UserId);
            return new CreateCheckoutSessionResult(false, null, null, ex.Message);
        }
    }

    public async Task<AttachPaymentMethodResult> AttachPaymentMethodAsync(string customerId, string paymentMethodId)
    {
        try
        {
            var paymentMethodService = new PaymentMethodService(stripeClient);
            await paymentMethodService.AttachAsync(paymentMethodId, new PaymentMethodAttachOptions
            {
                Customer = customerId
            });
            
            logger.LogInformation("Attached payment method {PaymentMethodId} to customer {CustomerId}", 
                paymentMethodId, customerId);
            
            return new AttachPaymentMethodResult(true, paymentMethodId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to attach payment method {PaymentMethodId}", paymentMethodId);
            return new AttachPaymentMethodResult(false, null, ex.Message);
        }
    }

    public async Task<DetachPaymentMethodResult> DetachPaymentMethodAsync(string paymentMethodId)
    {
        try
        {
            var paymentMethodService = new PaymentMethodService(stripeClient);
            await paymentMethodService.DetachAsync(paymentMethodId);
            
            logger.LogInformation("Detached payment method {PaymentMethodId}", paymentMethodId);
            
            return new DetachPaymentMethodResult(true);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to detach payment method {PaymentMethodId}", paymentMethodId);
            return new DetachPaymentMethodResult(false, ex.Message);
        }
    }

    public async Task<SetDefaultPaymentMethodResult> SetDefaultPaymentMethodAsync(string customerId, string paymentMethodId)
    {
        try
        {
            var customerService = new CustomerService(stripeClient);
            await customerService.UpdateAsync(customerId, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            });
            
            logger.LogInformation("Set default payment method {PaymentMethodId} for customer {CustomerId}", 
                paymentMethodId, customerId);
            
            return new SetDefaultPaymentMethodResult(true);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to set default payment method for customer {CustomerId}", customerId);
            return new SetDefaultPaymentMethodResult(false, ex.Message);
        }
    }

    public async Task<RefundPaymentResult> RefundPaymentAsync(string paymentId, decimal? amount = null)
    {
        try
        {
            var refundService = new RefundService(stripeClient);
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100);
            }

            var refund = await refundService.CreateAsync(options);
            
            logger.LogInformation("Created refund {RefundId} for payment {PaymentId}", 
                refund.Id, paymentId);
            
            return new RefundPaymentResult(
                true,
                refund.Id,
                refund.Amount / 100m
            );
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to refund payment {PaymentId}", paymentId);
            return new RefundPaymentResult(false, null, null, ex.Message);
        }
    }

    public Task<WebhookEventResult> HandleWebhookAsync(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                options.WebhookSecret
            );

            logger.LogInformation("Received Stripe webhook event: {EventType}", stripeEvent.Type);

            return Task.FromResult(new WebhookEventResult(true, stripeEvent.Type));
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to process Stripe webhook");
            return Task.FromResult(new WebhookEventResult(false, null, ex.Message));
        }
    }

    private static SubscriptionStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "trialing" => SubscriptionStatus.Trialing,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.Unpaid,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            "paused" => SubscriptionStatus.Paused,
            _ => SubscriptionStatus.Incomplete
        };
    }
}
