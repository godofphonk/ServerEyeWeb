namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

public class WebhookEventProcessor : BackgroundService
{
    private readonly IWebhookEventRepository webhookEventRepository;
    private readonly IOutboxMessageRepository outboxRepository;
    private readonly IPaymentRepository paymentRepository;
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly IPaymentProviderFactory providerFactory;
    private readonly ILogger<WebhookEventProcessor> logger;
    private readonly TimeSpan processingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan deadLetterThreshold = TimeSpan.FromMinutes(30);
    private readonly int maxRetries = 5;

    public WebhookEventProcessor(
        IWebhookEventRepository webhookEventRepository,
        IOutboxMessageRepository outboxRepository,
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IPaymentProviderFactory providerFactory,
        ILogger<WebhookEventProcessor> logger)
    {
        this.webhookEventRepository = webhookEventRepository;
        this.outboxRepository = outboxRepository;
        this.paymentRepository = paymentRepository;
        this.subscriptionRepository = subscriptionRepository;
        this.providerFactory = providerFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Webhook Event Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
                await ProcessOutboxMessagesAsync(stoppingToken);
                await MoveFailedEventsToDeadLetterAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in webhook event processor loop");
            }

            await Task.Delay(processingInterval, stoppingToken);
        }

        logger.LogInformation("Webhook Event Processor stopped");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken stoppingToken)
    {
        var pendingEvents = await webhookEventRepository.GetPendingEventsAsync(10);

        foreach (var webhookEvent in pendingEvents)
        {
            if (stoppingToken.IsCancellationRequested) break;

            await ProcessEventWithIdempotencyAsync(webhookEvent);
        }
    }

    private async Task ProcessEventWithIdempotencyAsync(WebhookEvent webhookEvent)
    {
        var eventId = webhookEvent.EventId;
        var lockKey = $"webhook:{eventId}";

        try
        {
            // Update status to Processing
            webhookEvent.Status = WebhookEventStatus.Processing;
            webhookEvent.UpdatedAt = DateTime.UtcNow;
            await webhookEventRepository.UpdateAsync(webhookEvent);

            logger.LogInformation(
                "Processing webhook event {EventId} of type {EventType} (Attempt {Attempt})",
                eventId,
                webhookEvent.EventType,
                webhookEvent.ProcessingAttempts + 1);

            // Parse the event data
            var provider = providerFactory.GetProvider(webhookEvent.Provider);
            var (eventType, data) = await provider.ParseWebhookEventAsync(webhookEvent.RawPayload);

            // Process based on event type with state machine validation
            var result = await ProcessEventByTypeAsync(webhookEvent.EventType, data, webhookEvent);

            if (result.IsSuccess)
            {
                webhookEvent.Status = WebhookEventStatus.Processed;
                webhookEvent.ProcessedAt = DateTime.UtcNow;
                webhookEvent.ProcessingError = null;

                logger.LogInformation(
                    "Successfully processed webhook event {EventId} of type {EventType}",
                    eventId,
                    webhookEvent.EventType);
            }
            else
            {
                webhookEvent.ProcessingAttempts++;
                webhookEvent.ProcessingError = result.Error;

                if (webhookEvent.ProcessingAttempts >= maxRetries)
                {
                    webhookEvent.Status = WebhookEventStatus.Failed;
                    logger.LogError(
                        "Webhook event {EventId} failed after {MaxRetries} attempts: {Error}",
                        eventId,
                        maxRetries,
                        result.Error);
                }
                else
                {
                    webhookEvent.Status = WebhookEventStatus.Received; // Retry
                    logger.LogWarning(
                        "Webhook event {EventId} failed (attempt {Attempt}/{MaxRetries}): {Error}",
                        eventId,
                        webhookEvent.ProcessingAttempts,
                        maxRetries,
                        result.Error);
                }
            }

            webhookEvent.UpdatedAt = DateTime.UtcNow;
            await webhookEventRepository.UpdateAsync(webhookEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception processing webhook event {EventId}", eventId);

            webhookEvent.ProcessingAttempts++;
            webhookEvent.ProcessingError = ex.Message;
            webhookEvent.Status = webhookEvent.ProcessingAttempts >= maxRetries
                ? WebhookEventStatus.Failed
                : WebhookEventStatus.Received;
            webhookEvent.UpdatedAt = DateTime.UtcNow;

            await webhookEventRepository.UpdateAsync(webhookEvent);
        }
    }

    private async Task<ProcessResult> ProcessEventByTypeAsync(string eventType, object data, WebhookEvent webhookEvent)
    {
        return eventType switch
        {
            "checkout.session.completed" => await ProcessCheckoutSessionCompletedAsync(data),
            "customer.subscription.created" => await ProcessSubscriptionCreatedAsync(data),
            "customer.subscription.updated" => await ProcessSubscriptionUpdatedAsync(data),
            "customer.subscription.deleted" => await ProcessSubscriptionDeletedAsync(data),
            "invoice.payment_succeeded" => await ProcessInvoicePaymentSucceededAsync(data),
            "invoice.payment_failed" => await ProcessInvoicePaymentFailedAsync(data),
            "payment_intent.succeeded" => await ProcessPaymentIntentSucceededAsync(data),
            "payment_intent.payment_failed" => await ProcessPaymentIntentFailedAsync(data),
            _ => ProcessResult.Success() // Unhandled event types
        };
    }

    private async Task<ProcessResult> ProcessCheckoutSessionCompletedAsync(object data)
    {
        try
        {
            var sessionId = StripeWebhookHelper.GetStringProperty(data, "Id");
            var customerId = StripeWebhookHelper.GetStringProperty(data, "CustomerId");
            var metadata = StripeWebhookHelper.GetMetadata(data);

            logger.LogInformation(
                "Processing checkout.session.completed: Session={SessionId}, Customer={CustomerId}",
                sessionId,
                customerId);

            // Extract user ID from metadata
            if (metadata == null || !metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                return ProcessResult.Failure("Missing or invalid user_id in metadata");
            }

            // Get subscription details from session
            var subscriptionId = StripeWebhookHelper.GetStringProperty(data, "SubscriptionId");

            // Create outbox message for side effects
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "CheckoutSessionCompleted",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    UserId = userId,
                    SessionId = sessionId,
                    SubscriptionId = subscriptionId,
                    CustomerId = customerId
                }),
                CreatedAt = DateTime.UtcNow
            };

            await outboxRepository.AddAsync(outboxMessage);

            logger.LogInformation(
                "Created outbox message for checkout session {SessionId}, user {UserId}",
                sessionId,
                userId);

            return ProcessResult.Success();
        }
        catch (Exception ex)
        {
            return ProcessResult.Failure($"Error processing checkout session: {ex.Message}");
        }
    }

    private async Task<ProcessResult> ProcessSubscriptionCreatedAsync(object data)
    {
        try
        {
            var subscriptionId = StripeWebhookHelper.GetStringProperty(data, "Id");
            var customerId = StripeWebhookHelper.GetStringProperty(data, "CustomerId");
            var metadata = StripeWebhookHelper.GetMetadata(data);
            var status = StripeWebhookHelper.GetStringProperty(data, "Status");

            logger.LogInformation(
                "Processing customer.subscription.created: Subscription={SubscriptionId}, Customer={CustomerId}, Status={Status}",
                subscriptionId,
                customerId,
                status);

            // Extract user ID from metadata
            if (metadata == null || !metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                return ProcessResult.Failure("Missing or invalid user_id in metadata");
            }

            // Get existing subscription
            var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
            if (subscription == null)
            {
                return ProcessResult.Failure($"No subscription found for user {userId}");
            }

            // Determine plan from Stripe subscription
            var planId = DeterminePlanIdFromStripeSubscription(data);

            // State machine validation
            if (!IsValidTransition(subscription.Status, SubscriptionStatus.Active))
            {
                return ProcessResult.Failure(
                    $"Invalid state transition from {subscription.Status} to Active");
            }

            // Update subscription
            subscription.PlanId = planId;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStart = DateTime.UtcNow;
            subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
            subscription.UpdatedAt = DateTime.UtcNow;

            await subscriptionRepository.UpdateAsync(subscription);

            logger.LogInformation(
                "Updated subscription {SubscriptionId} for user {UserId} to plan {PlanId}, status Active",
                subscription.Id,
                userId,
                planId);

            return ProcessResult.Success();
        }
        catch (Exception ex)
        {
            return ProcessResult.Failure($"Error processing subscription created: {ex.Message}");
        }
    }

    private async Task<ProcessResult> ProcessSubscriptionUpdatedAsync(object data)
    {
        try
        {
            var subscriptionId = StripeWebhookHelper.GetStringProperty(data, "Id");
            var metadata = StripeWebhookHelper.GetMetadata(data);
            var stripeStatus = StripeWebhookHelper.GetStringProperty(data, "Status");

            if (metadata == null || !metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                return ProcessResult.Failure("Missing or invalid user_id in metadata");
            }

            var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
            if (subscription == null)
            {
                return ProcessResult.Failure($"No subscription found for user {userId}");
            }

            var newStatus = MapStripeStatus(stripeStatus);
            var planId = DeterminePlanIdFromStripeSubscription(data);

            // State machine validation
            if (!IsValidTransition(subscription.Status, newStatus))
            {
                logger.LogWarning(
                    "Invalid state transition from {CurrentStatus} to {NewStatus} for subscription {SubscriptionId}. Skipping.",
                    subscription.Status,
                    newStatus,
                    subscription.Id);
                return ProcessResult.Success(); // Skip but don't fail
            }

            subscription.PlanId = planId;
            subscription.Status = newStatus;
            subscription.UpdatedAt = DateTime.UtcNow;

            await subscriptionRepository.UpdateAsync(subscription);

            logger.LogInformation(
                "Updated subscription {SubscriptionId} for user {UserId} to status {Status}",
                subscription.Id,
                userId,
                newStatus);

            return ProcessResult.Success();
        }
        catch (Exception ex)
        {
            return ProcessResult.Failure($"Error processing subscription updated: {ex.Message}");
        }
    }

    private async Task<ProcessResult> ProcessSubscriptionDeletedAsync(object data)
    {
        try
        {
            var metadata = StripeWebhookHelper.GetMetadata(data);

            if (metadata == null || !metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                return ProcessResult.Failure("Missing or invalid user_id in metadata");
            }

            var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
            if (subscription == null)
            {
                return ProcessResult.Failure($"No subscription found for user {userId}");
            }

            // State machine validation
            if (!IsValidTransition(subscription.Status, SubscriptionStatus.Canceled))
            {
                return ProcessResult.Failure(
                    $"Invalid state transition from {subscription.Status} to Canceled");
            }

            subscription.Status = SubscriptionStatus.Canceled;
            subscription.UpdatedAt = DateTime.UtcNow;

            await subscriptionRepository.UpdateAsync(subscription);

            logger.LogInformation(
                "Canceled subscription {SubscriptionId} for user {UserId}",
                subscription.Id,
                userId);

            return ProcessResult.Success();
        }
        catch (Exception ex)
        {
            return ProcessResult.Failure($"Error processing subscription deleted: {ex.Message}");
        }
    }

    private async Task<ProcessResult> ProcessInvoicePaymentSucceededAsync(object data)
    {
        try
        {
            var invoiceId = StripeWebhookHelper.GetStringProperty(data, "Id");
            var customerId = StripeWebhookHelper.GetStringProperty(data, "CustomerId");
            var amountPaid = StripeWebhookHelper.GetLongProperty(data, "AmountPaid") / 100.0m;
            var currency = StripeWebhookHelper.GetStringProperty(data, "Currency");
            var metadata = StripeWebhookHelper.GetMetadata(data);

            logger.LogInformation(
                "Processing invoice.payment_succeeded: Invoice={InvoiceId}, Customer={CustomerId}, Amount={Amount} {Currency}",
                invoiceId,
                customerId,
                amountPaid,
                currency);

            // Extract user ID from metadata
            if (metadata == null || !metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                return ProcessResult.Failure("Missing or invalid user_id in metadata");
            }

            // Idempotency check - already processed?
            var existingPayment = await paymentRepository.GetByProviderPaymentIdAsync(invoiceId);
            if (existingPayment != null)
            {
                logger.LogInformation(
                    "Payment for invoice {InvoiceId} already exists (idempotency check)",
                    invoiceId);
                return ProcessResult.Success();
            }

            // Get subscription
            var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
            if (subscription == null)
            {
                return ProcessResult.Failure($"No subscription found for user {userId}");
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
                "Created payment {PaymentId} for user {UserId}, invoice {InvoiceId}, amount {Amount} {Currency}",
                payment.Id,
                userId,
                invoiceId,
                amountPaid,
                currency);

            // Extend subscription period
            subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
            subscription.UpdatedAt = DateTime.UtcNow;
            await subscriptionRepository.UpdateAsync(subscription);

            logger.LogInformation(
                "Extended subscription {SubscriptionId} period to {PeriodEnd}",
                subscription.Id,
                subscription.CurrentPeriodEnd);

            return ProcessResult.Success();
        }
        catch (Exception ex)
        {
            return ProcessResult.Failure($"Error processing invoice payment: {ex.Message}");
        }
    }

    private async Task<ProcessResult> ProcessInvoicePaymentFailedAsync(object data)
    {
        try
        {
            var invoiceId = StripeWebhookHelper.GetStringProperty(data, "Id");
            var metadata = StripeWebhookHelper.GetMetadata(data);

            if (metadata == null || !metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                return ProcessResult.Failure("Missing or invalid user_id in metadata");
            }

            var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
            if (subscription == null)
            {
                return ProcessResult.Failure($"No subscription found for user {userId}");
            }

            // State machine validation
            if (!IsValidTransition(subscription.Status, SubscriptionStatus.PastDue))
            {
                return ProcessResult.Failure(
                    $"Invalid state transition from {subscription.Status} to PastDue");
            }

            subscription.Status = SubscriptionStatus.PastDue;
            subscription.UpdatedAt = DateTime.UtcNow;

            await subscriptionRepository.UpdateAsync(subscription);

            logger.LogWarning(
                "Subscription {SubscriptionId} for user {UserId} marked as PastDue due to failed invoice payment",
                subscription.Id,
                userId);

            return ProcessResult.Success();
        }
        catch (Exception ex)
        {
            return ProcessResult.Failure($"Error processing invoice payment failed: {ex.Message}");
        }
    }

    private Task<ProcessResult> ProcessPaymentIntentSucceededAsync(object data)
    {
        logger.LogInformation("Processing payment_intent.succeeded");
        return Task.FromResult(ProcessResult.Success());
    }

    private Task<ProcessResult> ProcessPaymentIntentFailedAsync(object data)
    {
        logger.LogWarning("Processing payment_intent.payment_failed");
        return Task.FromResult(ProcessResult.Success());
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        var pendingMessages = await outboxRepository.GetPendingMessagesAsync(10);

        foreach (var message in pendingMessages)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                logger.LogInformation(
                    "Processing outbox message {MessageId} of type {MessageType}",
                    message.Id,
                    message.MessageType);

                // Process side effects here (emails, notifications, etc.)
                // This is where you'd send emails, update external systems, etc.

                await outboxRepository.MarkAsProcessedAsync(message.Id);

                logger.LogInformation(
                    "Successfully processed outbox message {MessageId}",
                    message.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);

                await outboxRepository.IncrementRetryCountAsync(message.Id);

                if (message.RetryCount >= 3)
                {
                    await outboxRepository.MarkAsFailedAsync(message.Id, ex.Message);
                }
            }
        }
    }

    private async Task MoveFailedEventsToDeadLetterAsync(CancellationToken stoppingToken)
    {
        var failedEvents = await webhookEventRepository.GetFailedEventsAsync(
            maxRetries,
            deadLetterThreshold);

        foreach (var webhookEvent in failedEvents)
        {
            webhookEvent.Status = WebhookEventStatus.DeadLetter;
            webhookEvent.UpdatedAt = DateTime.UtcNow;
            await webhookEventRepository.UpdateAsync(webhookEvent);

            logger.LogError(
                "Moved webhook event {EventId} to dead letter queue after {Attempts} attempts",
                webhookEvent.EventId,
                webhookEvent.ProcessingAttempts);
        }
    }

    private Guid DeterminePlanIdFromStripeSubscription(object data)
    {
        var freePlanId = Guid.Parse("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c");
        var proPlanId = Guid.Parse("841bb3db-424c-46e5-a752-04641391c993");

        var priceId = StripeWebhookHelper.GetFirstPriceId(data);

        if (!string.IsNullOrEmpty(priceId))
        {
            logger.LogDebug("Stripe subscription has price ID: {PriceId}", priceId);

            return priceId switch
            {
                "price_1TCvgkFpa5lei83s50BoBxyV" => proPlanId,
                "price_1TCvhQFpa5lei83sVjR0nHDm" => proPlanId,
                _ => freePlanId
            };
        }

        return freePlanId;
    }

    private static SubscriptionStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus?.ToUpperInvariant() switch
        {
            "ACTIVE" => SubscriptionStatus.Active,
            "CANCELED" => SubscriptionStatus.Canceled,
            "INCOMPLETE" => SubscriptionStatus.PastDue,
            "INCOMPLETE_EXPIRED" => SubscriptionStatus.Canceled,
            "PAST_DUE" => SubscriptionStatus.PastDue,
            "TRIALING" => SubscriptionStatus.Active,
            "UNPAID" => SubscriptionStatus.PastDue,
            _ => SubscriptionStatus.Active
        };
    }

    private static bool IsValidTransition(SubscriptionStatus current, SubscriptionStatus next)
    {
        // Define valid state transitions
        return (current, next) switch
        {
            // Free can go to Active (upgrade)
            (SubscriptionStatus.Free, SubscriptionStatus.Active) => true,
            // Active can go to PastDue (payment failed)
            (SubscriptionStatus.Active, SubscriptionStatus.PastDue) => true,
            // Active can go to Canceled (cancellation)
            (SubscriptionStatus.Active, SubscriptionStatus.Canceled) => true,
            // PastDue can go to Active (payment recovered)
            (SubscriptionStatus.PastDue, SubscriptionStatus.Active) => true,
            // PastDue can go to Canceled (final cancellation)
            (SubscriptionStatus.PastDue, SubscriptionStatus.Canceled) => true,
            // Same state is valid (idempotent)
            (var s1, var s2) when s1 == s2 => true,
            // All other transitions are invalid
            _ => false
        };
    }
}

public class ProcessResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }

    public static ProcessResult Success() => new() { IsSuccess = true };
    public static ProcessResult Failure(string error) => new() { IsSuccess = false, Error = error };
}
