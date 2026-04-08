namespace ServerEye.API.Controllers.Billing;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;
using ServerEye.Infrastructure.ExternalServices.Stripe;
using ServerEye.Infrastructure.ExternalServices.YooKassa;

[ApiController]
[Route("api/billing/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookEventRepository webhookEventRepository;
    private readonly IWebhookService webhookService;
    private readonly IPaymentProviderFactory providerFactory;
    private readonly StripeConfiguration stripeConfig;
    private readonly YooKassaConfiguration yookassaConfig;
    private readonly ILogger<WebhookController> logger;

    public WebhookController(
        IWebhookEventRepository webhookEventRepository,
        IWebhookService webhookService,
        IPaymentProviderFactory providerFactory,
        IOptions<StripeConfiguration> stripeConfig,
        IOptions<YooKassaConfiguration> yookassaConfig,
        ILogger<WebhookController> logger)
    {
        this.webhookEventRepository = webhookEventRepository;
        this.webhookService = webhookService;
        this.providerFactory = providerFactory;
        this.stripeConfig = stripeConfig.Value;
        this.yookassaConfig = yookassaConfig.Value;
        this.logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Read raw payload
            string rawPayload;
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                rawPayload = await reader.ReadToEndAsync();
            }

            // Fast validation - minimal checks
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                logger.LogWarning("Stripe webhook received with empty payload");
                return BadRequest(new { message = "Empty payload" });
            }

            var signature = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("Stripe webhook received without signature");
                return BadRequest(new { message = "Missing signature" });
            }

            logger.LogInformation(
                "Received Stripe webhook, payload size: {Size} bytes",
                rawPayload.Length);

            // Verify signature (fail fast if invalid)
            var provider = providerFactory.GetProvider(PaymentProvider.Stripe);
            var isValid = await provider.VerifyWebhookSignatureAsync(
                rawPayload,
                signature,
                stripeConfig.WebhookSecret);

            if (!isValid)
            {
                logger.LogWarning("Invalid Stripe webhook signature - rejecting");
                return Unauthorized(new { message = "Invalid signature" });
            }

            // Extract event ID and type from payload (fast parse)
            string eventId;
            string eventType;
            try
            {
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(rawPayload);
                var root = jsonDoc.RootElement;
                eventId = root.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                eventType = root.GetProperty("type").GetString() ?? "unknown";
            }
            catch
            {
                logger.LogWarning("Failed to parse Stripe event ID from payload");
                return BadRequest(new { message = "Invalid payload structure" });
            }

            // Check for duplicate (idempotency) - fast check
            var existingEvent = await webhookEventRepository.GetByEventIdAsync(eventId);
            if (existingEvent != null)
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "Stripe webhook event {EventId} already received (idempotency check), returning 200 in {ElapsedMs}ms",
                    eventId,
                    stopwatch.ElapsedMilliseconds);
                return Ok(new { received = true, duplicate = true });
            }

            // Persist raw event immediately (no processing)
            var webhookEvent = new WebhookEvent
            {
                Id = Guid.NewGuid(),
                Provider = PaymentProvider.Stripe,
                EventId = eventId,
                EventType = eventType,
                RawPayload = rawPayload,
                Headers = $"Stripe-Signature: {signature}",
                Status = WebhookEventStatus.Received,
                ProcessingAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await webhookEventRepository.AddAsync(webhookEvent);

            // FAST ACKNOWLEDGMENT: Return 200 immediately
            // All processing happens asynchronously in WebhookEventProcessor
            stopwatch.Stop();

            logger.LogInformation(
                "Stripe webhook {EventId} persisted and acknowledged in {ElapsedMs}ms. Processing will continue asynchronously.",
                eventId,
                stopwatch.ElapsedMilliseconds);

            return Ok(new { received = true, eventId = eventId });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error handling Stripe webhook after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("yookassa")]
    public async Task<IActionResult> HandleYooKassaWebhook()
    {
        try
        {
            string payload;
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            // Validate JSON payload
            if (string.IsNullOrWhiteSpace(payload))
            {
                this.logger.LogWarning("YooKassa webhook received with empty payload");
                return BadRequest(new { message = "Empty payload" });
            }

            try
            {
                System.Text.Json.JsonDocument.Parse(payload);
            }
            catch (System.Text.Json.JsonException)
            {
                this.logger.LogWarning("YooKassa webhook received with invalid JSON");
                return BadRequest(new { message = "Invalid JSON payload" });
            }

            var signature = Request.Headers["Content-HMAC"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                this.logger.LogWarning("YooKassa webhook received without signature");
                return BadRequest(new { message = "Missing signature" });
            }

            this.logger.LogInformation("Processing YooKassa webhook, payload size: {Size} bytes", payload.Length);

            var provider = providerFactory.GetProvider(PaymentProvider.YooKassa);
            var isValid = await provider.VerifyWebhookSignatureAsync(
                payload,
                signature,
                this.yookassaConfig.WebhookSecret);

            if (!isValid)
            {
                this.logger.LogWarning("Invalid YooKassa webhook signature");
                return Unauthorized(new { message = "Invalid signature" });
            }

            this.logger.LogInformation("YooKassa webhook signature verified successfully");

            var success = await webhookService.ProcessWebhookAsync(
                PaymentProvider.YooKassa,
                payload,
                signature);

            if (success)
            {
                return Ok();
            }

            return StatusCode(500, new { message = "Failed to process webhook" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error handling YooKassa webhook");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
