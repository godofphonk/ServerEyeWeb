namespace ServerEye.API.Controllers.Billing;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services.Billing;
using ServerEye.Infrastructure.ExternalServices.Stripe;
using ServerEye.Infrastructure.ExternalServices.YooKassa;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService webhookService;
    private readonly IPaymentProviderFactory providerFactory;
    private readonly StripeConfiguration stripeConfig;
    private readonly YooKassaConfiguration yookassaConfig;
    private readonly ILogger<WebhookController> logger;

    public WebhookController(
        IWebhookService webhookService,
        IPaymentProviderFactory providerFactory,
        IOptions<StripeConfiguration> stripeConfig,
        IOptions<YooKassaConfiguration> yookassaConfig,
        ILogger<WebhookController> logger)
    {
        this.webhookService = webhookService;
        this.providerFactory = providerFactory;
        this.stripeConfig = stripeConfig.Value;
        this.yookassaConfig = yookassaConfig.Value;
        this.logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        try
        {
            string payload;
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }
            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("Stripe webhook received without signature");
                return BadRequest(new { message = "Missing signature" });
            }

            this.logger.LogInformation("Processing Stripe webhook, payload size: {Size} bytes", payload.Length);
            
            var provider = providerFactory.GetProvider(PaymentProvider.Stripe);
            var isValid = await provider.VerifyWebhookSignatureAsync(
                payload,
                signature,
                stripeConfig.WebhookSecret);

            if (!isValid)
            {
                logger.LogWarning("Invalid Stripe webhook signature");
                return Unauthorized(new { message = "Invalid signature" });
            }

            this.logger.LogInformation("Stripe webhook signature verified successfully");
            
            var success = await webhookService.ProcessWebhookAsync(
                PaymentProvider.Stripe,
                payload,
                signature);

            if (success)
            {
                this.logger.LogInformation("Stripe webhook processed successfully");
                return Ok();
            }

            this.logger.LogError("Failed to process Stripe webhook");
            return StatusCode(500, new { message = "Failed to process webhook" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling Stripe webhook");
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
            var signature = Request.Headers["X-YooKassa-Signature"].ToString();

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
