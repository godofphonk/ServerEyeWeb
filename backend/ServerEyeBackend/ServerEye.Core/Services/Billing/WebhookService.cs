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
    private readonly IPaymentProviderFactory providerFactory;
    private readonly ILogger<WebhookService> logger;

    public WebhookService(
        IWebhookEventRepository webhookEventRepository,
        IPaymentProviderFactory providerFactory,
        ILogger<WebhookService> logger)
    {
        this.webhookEventRepository = webhookEventRepository;
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
                Payload = payload,
                IsProcessed = false,
                ProcessingError = null,
                ProcessingAttempts = 0,
                ProcessedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await webhookEventRepository.AddAsync(webhookEvent);

            await ProcessWebhookEventAsync(webhookEvent, data);

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
                var (eventType, data) = await provider.ParseWebhookEventAsync(webhookEvent.Payload);

                await ProcessWebhookEventAsync(webhookEvent, data);

                webhookEvent.IsProcessed = true;
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

    private Task HandleSubscriptionCreatedAsync(object data)
    {
        _ = data;

        logger.LogInformation("Handling subscription created");

        // Business metric: New subscription
        logger.LogInformation(
            "Subscription lifecycle: New subscription created");

        return Task.CompletedTask;
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

    private Task HandleInvoicePaymentSucceededAsync(object data)
    {
        _ = data;

        logger.LogInformation("Handling invoice payment succeeded");

        // Business metric: Revenue recognition
        logger.LogInformation(
            "Revenue recognition: Invoice payment succeeded");

        return Task.CompletedTask;
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
