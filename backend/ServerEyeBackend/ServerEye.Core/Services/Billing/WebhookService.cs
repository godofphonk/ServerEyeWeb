namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

public class WebhookService : IWebhookService
{
    private readonly IWebhookEventRepository webhookRepository;
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly IPaymentRepository paymentRepository;
    private readonly IPaymentProviderFactory providerFactory;
    private readonly ILogger<WebhookService> logger;

    public WebhookService(
        IWebhookEventRepository webhookRepository,
        ISubscriptionRepository subscriptionRepository,
        IPaymentRepository paymentRepository,
        IPaymentProviderFactory providerFactory,
        ILogger<WebhookService> logger)
    {
        this.webhookRepository = webhookRepository;
        this.subscriptionRepository = subscriptionRepository;
        this.paymentRepository = paymentRepository;
        this.providerFactory = providerFactory;
        this.logger = logger;
    }

    public async Task<bool> ProcessWebhookAsync(
        PaymentProvider provider,
        string payload,
        string signature)
    {
        logger.LogInformation("Processing webhook from {Provider}", provider);

        try
        {
            var paymentProvider = providerFactory.GetProvider(provider);

            var (eventType, data) = await paymentProvider.ParseWebhookEventAsync(payload);

            var existingEvent = await webhookRepository.GetByEventIdAsync(eventType);
            if (existingEvent != null)
            {
                logger.LogWarning("Webhook event {EventId} already processed", eventType);
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
                ProcessingAttempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            await webhookRepository.AddAsync(webhookEvent);

            await ProcessWebhookEventAsync(webhookEvent, data);

            webhookEvent.IsProcessed = true;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            await webhookRepository.UpdateAsync(webhookEvent);

            logger.LogInformation("Successfully processed webhook event {EventId}", eventType);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process webhook from {Provider}", provider);
            return false;
        }
    }

    public async Task ProcessPendingWebhooksAsync()
    {
        logger.LogInformation("Processing pending webhooks");

        var pendingEvents = await webhookRepository.GetUnprocessedAsync();

        foreach (var webhookEvent in pendingEvents)
        {
            try
            {
                var provider = providerFactory.GetProvider(webhookEvent.Provider);
                var (eventType, data) = await provider.ParseWebhookEventAsync(webhookEvent.Payload);

                await ProcessWebhookEventAsync(webhookEvent, data);

                webhookEvent.IsProcessed = true;
                webhookEvent.ProcessedAt = DateTime.UtcNow;
                await webhookRepository.UpdateAsync(webhookEvent);

                logger.LogInformation("Processed pending webhook event {EventId}", webhookEvent.EventId);
            }
            catch (Exception ex)
            {
                webhookEvent.ProcessingAttempts++;
                webhookEvent.ProcessingError = ex.Message;
                await webhookRepository.UpdateAsync(webhookEvent);

                logger.LogError(ex, "Failed to process pending webhook event {EventId}, attempt {Attempt}", 
                    webhookEvent.EventId, webhookEvent.ProcessingAttempts);
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

    private async Task HandleCheckoutSessionCompletedAsync(object data)
    {
        logger.LogInformation("Handling checkout session completed");
    }

    private async Task HandleSubscriptionCreatedAsync(object data)
    {
        logger.LogInformation("Handling subscription created");
    }

    private async Task HandleSubscriptionUpdatedAsync(object data)
    {
        logger.LogInformation("Handling subscription updated");
    }

    private async Task HandleSubscriptionDeletedAsync(object data)
    {
        logger.LogInformation("Handling subscription deleted");
    }

    private async Task HandleInvoicePaymentSucceededAsync(object data)
    {
        logger.LogInformation("Handling invoice payment succeeded");
    }

    private async Task HandleInvoicePaymentFailedAsync(object data)
    {
        logger.LogInformation("Handling invoice payment failed");
    }

    private async Task HandlePaymentIntentSucceededAsync(object data)
    {
        logger.LogInformation("Handling payment intent succeeded");
    }

    private async Task HandlePaymentIntentFailedAsync(object data)
    {
        logger.LogInformation("Handling payment intent failed");
    }
}
