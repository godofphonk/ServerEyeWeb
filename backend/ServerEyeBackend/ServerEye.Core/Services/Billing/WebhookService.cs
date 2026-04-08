using System.Diagnostics;
namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

public class WebhookService : IWebhookService
{
    private readonly IWebhookEventRepository webhookEventRepository;
    private readonly IPaymentRepository paymentRepository;
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly IPaymentProviderFactory providerFactory;
    private readonly ILogger<WebhookService> logger;

    public WebhookService(
        IWebhookEventRepository webhookEventRepository,
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IPaymentProviderFactory providerFactory,
        ILogger<WebhookService> logger)
    {
        this.webhookEventRepository = webhookEventRepository;
        this.paymentRepository = paymentRepository;
        this.subscriptionRepository = subscriptionRepository;
        this.providerFactory = providerFactory;
        this.logger = logger;
    }

    public async Task<bool> ProcessWebhookAsync(
        PaymentProvider provider,
        string payload,
        string signature)
    {
        logger.LogInformation("Processing webhook from {Provider}, payload size: {Size} bytes", provider, payload.Length);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var paymentProvider = providerFactory.GetProvider(provider);

            var (eventType, data) = await paymentProvider.ParseWebhookEventAsync(payload);

            var existingEvent = await webhookEventRepository.GetByEventIdAsync(eventType);
            if (existingEvent != null)
            {
                stopwatch.Stop();
                logger.LogWarning("Webhook event {EventId} already processed in {ElapsedMs}ms", eventType, stopwatch.ElapsedMilliseconds);
                return true;
            }

            var webhookEvent = new WebhookEvent
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                EventId = eventType,
                EventType = eventType,
                RawPayload = payload,
                Status = Entities.Billing.WebhookEventStatus.Received,
                ProcessingError = null,
                ProcessingAttempts = 0,
                ProcessedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await webhookEventRepository.AddAsync(webhookEvent);

            logger.LogInformation("About to call ProcessWebhookEventAsync with event type: {EventType}", webhookEvent.EventType);

            await ProcessWebhookEventAsync(webhookEvent, data);

            logger.LogInformation("ProcessWebhookEventAsync completed successfully");

            webhookEvent.ProcessedAt = DateTime.UtcNow;
            await webhookEventRepository.UpdateAsync(webhookEvent);

            stopwatch.Stop();

            logger.LogInformation(
                "Webhook processed: {Provider} {EventType} in {ElapsedMs}ms",
                provider,
                eventType,
                stopwatch.ElapsedMilliseconds);

            // Business metric: Webhook conversion tracking
            logger.LogInformation(
                "Webhook conversion: {Provider} {EventType} processed successfully",
                provider,
                eventType);

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(ex, "Failed to process webhook from {Provider} in {ElapsedMs}ms: {ErrorType}", provider, stopwatch.ElapsedMilliseconds, ex.GetType().Name);

            // Business metric: Webhook failure tracking
            logger.LogWarning(
                "Webhook failure: {Provider} {EventType}, reason {ErrorType}",
                provider,
                "unknown",
                ex.GetType().Name);

            return false;
        }
    }

    public async Task ProcessPendingWebhooksAsync()
    {
        logger.LogInformation("Processing pending webhooks");

        var pendingEvents = await webhookEventRepository.GetUnprocessedAsync();

        foreach (var webhookEvent in pendingEvents)
        {
            try
            {
                var provider = providerFactory.GetProvider(webhookEvent.Provider);
                var (eventType, data) = await provider.ParseWebhookEventAsync(webhookEvent.RawPayload);

                await ProcessWebhookEventAsync(webhookEvent, data);

                webhookEvent.Status = Entities.Billing.WebhookEventStatus.Processed;
                webhookEvent.ProcessedAt = DateTime.UtcNow;
                await webhookEventRepository.UpdateAsync(webhookEvent);

                logger.LogInformation("Processed pending webhook event {EventId}", webhookEvent.EventId);
            }
            catch (Exception ex)
            {
                webhookEvent.ProcessingAttempts++;
                webhookEvent.ProcessingError = ex.Message;
                await webhookEventRepository.UpdateAsync(webhookEvent);

                logger.LogInformation(
                    "Processing webhook event {EventId} of type {EventType}",
                    webhookEvent.EventId,
                    webhookEvent.EventType);
            }
        }
    }

    private async Task ProcessWebhookEventAsync(WebhookEvent webhookEvent, object data)
    {
        logger.LogInformation("Processing webhook event type {EventType}", webhookEvent.EventType);

        switch (webhookEvent.EventType)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(data);
                break;

            case "customer.subscription.created":
                await HandleSubscriptionCreatedAsync(data);
                break;

            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(data);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(data);
                break;

            case "invoice.payment_succeeded":
                await HandleInvoicePaymentSucceededAsync(data);
                break;

            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(data);
                break;

            case "payment_intent.succeeded":
                await HandlePaymentIntentSucceededAsync(data);
                break;

            case "payment_intent.payment_failed":
                await HandlePaymentIntentFailedAsync(data);
                break;

            default:
                logger.LogWarning("Unhandled webhook event type {EventType}", webhookEvent.EventType);
                break;
        }
    }

    private Task HandleCheckoutSessionCompletedAsync(object data)
    {
        _ = data;

        logger.LogInformation("Handling checkout session completed");

        // Business metric: Conversion tracking
        logger.LogInformation(
            "Conversion funnel: Checkout completed successfully");

        return Task.CompletedTask;
    }

    private async Task HandleSubscriptionCreatedAsync(object data)
    {
        logger.LogInformation("Handling subscription created");

        try
        {
            var subscriptionId = StripeWebhookHelper.GetStringProperty(data, "Id");
            var customerId = StripeWebhookHelper.GetStringProperty(data, "CustomerId");
            var metadata = StripeWebhookHelper.GetMetadata(data);

            logger.LogInformation(
                "Processing subscription {SubscriptionId} for customer {CustomerId}",
                subscriptionId,
                customerId);

            // Extract user ID from metadata
            if (metadata == null || !metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                logger.LogWarning("Subscription {SubscriptionId} missing user_id in metadata", subscriptionId);
                return;
            }

            // Get existing subscription
            var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
            if (subscription == null)
            {
                logger.LogWarning("No subscription found for user {UserId}", userId);
                return;
            }

            // Determine plan from Stripe subscription
            var planId = DeterminePlanIdFromStripeSubscription(data);
            var currentPeriodStart = StripeWebhookHelper.GetProperty(data, "CurrentPeriodStart");
            var currentPeriodEnd = StripeWebhookHelper.GetProperty(data, "CurrentPeriodEnd");
            var cancelAtPeriodEnd = StripeWebhookHelper.GetProperty(data, "CancelAtPeriodEnd");

            // Update subscription
            subscription.PlanId = planId;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStart = currentPeriodStart as DateTime?;
            subscription.CurrentPeriodEnd = currentPeriodEnd as DateTime?;
            subscription.CancelAtPeriodEnd = cancelAtPeriodEnd as bool? ?? false;
            subscription.UpdatedAt = DateTime.UtcNow;

            await subscriptionRepository.UpdateAsync(subscription);

            logger.LogInformation(
                "Updated subscription {SubscriptionId} for user {UserId} to plan {PlanId}",
                subscription.Id,
                userId,
                planId);

            // Business metric: New subscription
            logger.LogInformation(
                "Subscription lifecycle: New subscription created for user {UserId}, plan {PlanId}",
                userId,
                planId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling subscription created");
            throw;
        }
    }

    private Guid DeterminePlanIdFromStripeSubscription(object data)
    {
        // Known plan IDs from database
        var freePlanId = Guid.Parse("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c");
        var proPlanId = Guid.Parse("841bb3db-424c-46e5-a752-04641391c993");

        // Get first price ID from subscription items
        var priceId = StripeWebhookHelper.GetFirstPriceId(data);
        
        if (!string.IsNullOrEmpty(priceId))
        {
            logger.LogInformation("Stripe subscription has price ID: {PriceId}", priceId);

            // Map Stripe price IDs to our plan IDs
            return priceId switch
            {
                "price_1TCvgkFpa5lei83s50BoBxyV" => proPlanId, // Pro Monthly
                "price_1TCvhQFpa5lei83sVjR0nHDm" => proPlanId, // Pro Yearly
                _ => freePlanId // Default to Free
            };
        }

        return freePlanId;
    }

    private Task HandleSubscriptionUpdatedAsync(object data)
    {
        _ = data;

        logger.LogInformation("Handling subscription updated");

        // Business metric: Subscription change
        logger.LogInformation(
            "Subscription lifecycle: Subscription updated");

        return Task.CompletedTask;
    }

    private Task HandleSubscriptionDeletedAsync(object data)
    {
        _ = data;

        logger.LogInformation("Handling subscription deleted");

        // Business metric: Churn tracking
        logger.LogInformation(
            "Churn tracking: Subscription deleted/cancelled");

        return Task.CompletedTask;
    }

    private async Task HandleInvoicePaymentSucceededAsync(object data)
    {
        logger.LogInformation("Handling invoice payment succeeded");

        try
        {
            // Extract data using helper
            var invoiceId = StripeWebhookHelper.GetStringProperty(data, "Id");
            var customerId = StripeWebhookHelper.GetStringProperty(data, "CustomerId");
            var amountPaid = StripeWebhookHelper.GetLongProperty(data, "AmountPaid") / 100.0m;
            var currency = StripeWebhookHelper.GetStringProperty(data, "Currency");
            var metadata = StripeWebhookHelper.GetMetadata(data);

            logger.LogInformation(
                "Processing invoice {InvoiceId} for customer {CustomerId}, amount: {Amount} {Currency}",
                invoiceId,
                customerId,
                amountPaid,
                currency);

            // Extract user ID from metadata
            if (metadata == null || !metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                logger.LogWarning("Invoice {InvoiceId} missing user_id in metadata", invoiceId);
                return;
            }

            // Check if payment already exists
            var existingPayment = await paymentRepository.GetByProviderPaymentIdAsync(invoiceId);
            if (existingPayment != null)
            {
                logger.LogInformation("Payment for invoice {InvoiceId} already exists", invoiceId);
                return;
            }

            // Get subscription
            var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
            if (subscription == null)
            {
                logger.LogWarning("No subscription found for user {UserId}", userId);
                return;
            }

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = subscription.Id,
                Provider = PaymentProvider.Stripe,
                ProviderPaymentId = invoiceId,
                ProviderPaymentIntentId = StripeWebhookHelper.GetStringProperty(data, "PaymentIntentId"),
                Amount = amountPaid,
                Currency = currency,
                Status = PaymentStatus.Succeeded,
                ReceiptUrl = StripeWebhookHelper.GetStringProperty(data, "HostedInvoiceUrl"),
                InvoiceUrl = StripeWebhookHelper.GetStringProperty(data, "InvoicePdf"),
                Metadata = new Dictionary<string, string>
                {
                    ["stripe_invoice_id"] = invoiceId,
                    ["stripe_customer_id"] = customerId,
                    ["subscription_id"] = StripeWebhookHelper.GetStringProperty(data, "SubscriptionId")
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await paymentRepository.AddAsync(payment);

            logger.LogInformation(
                "Created payment {PaymentId} for user {UserId}, amount: {Amount} {Currency}",
                payment.Id,
                userId,
                payment.Amount,
                payment.Currency);

            // Business metric: Revenue recognition
            logger.LogInformation(
                "Revenue recognition: Invoice payment succeeded for user {UserId}, amount {Amount} {Currency}",
                userId,
                payment.Amount,
                payment.Currency);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling invoice payment succeeded");
            throw;
        }
    }

    private Task HandleInvoicePaymentFailedAsync(object data)
    {
        _ = data;

        logger.LogInformation("Handling invoice payment failed");

        // Business metric: Payment failure
        logger.LogWarning(
            "Payment failure: Invoice payment failed - potential churn risk");

        return Task.CompletedTask;
    }

    private Task HandlePaymentIntentSucceededAsync(object data)
    {
        _ = data;

        logger.LogInformation("Handling payment intent succeeded");

        // Business metric: Payment success
        logger.LogInformation(
            "Payment success: Payment intent completed");

        return Task.CompletedTask;
    }

    private Task HandlePaymentIntentFailedAsync(object data)
    {
        _ = data;

        logger.LogInformation("Handling payment intent failed");

        // Business metric: Payment failure
        logger.LogWarning(
            "Payment failure: Payment intent failed - conversion lost");

        return Task.CompletedTask;
    }
}
