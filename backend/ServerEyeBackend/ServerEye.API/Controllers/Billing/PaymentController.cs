namespace ServerEye.API.Controllers.Billing;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService paymentService;
    private readonly ILogger<PaymentController> logger;

    public PaymentController(
        IPaymentService paymentService,
        ILogger<PaymentController> logger)
    {
        this.paymentService = paymentService;
        this.logger = logger;
    }

    [HttpPost("intent")]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreatePaymentIntent(
        [FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var userId = GetUserId();
            this.logger.LogInformation("Creating payment intent for user: {UserId}, amount: {Amount}, currency: {Currency}", userId, request.Amount, request.Currency);

            var response = await paymentService.CreatePaymentIntentAsync(userId, request);

            this.logger.LogInformation("Payment intent created successfully: {ClientSecret}", response.ClientSecret?[..20] + "...");
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while creating payment intent");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment intent");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<PaymentDto>>> GetPaymentHistory(
        [FromQuery] int limit = 50)
    {
        try
        {
            var userId = GetUserId();
            this.logger.LogDebug("Getting payment history for user: {UserId}, limit: {Limit}", userId, limit);

            var payments = await paymentService.GetUserPaymentsAsync(userId, limit);

            this.logger.LogDebug("Retrieved {Count} payments for user: {UserId}", payments.Count, userId);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payment history");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{paymentId}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(Guid paymentId)
    {
        try
        {
            var payment = await paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            var userId = GetUserId();
            if (payment.UserId != userId)
            {
                return Forbid();
            }

            return Ok(payment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payment");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{paymentId}/refund")]
    public async Task<IActionResult> RefundPayment(
        Guid paymentId,
        [FromBody] RefundPaymentRequest? request = null)
    {
        try
        {
            var payment = await paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            var userId = GetUserId();
            if (payment.UserId != userId)
            {
                this.logger.LogWarning("Unauthorized refund attempt: user {UserId} tried to refund payment {PaymentId} belonging to {OwnerId}", userId, paymentId, payment.UserId);
                return Forbid();
            }

            this.logger.LogWarning("Refund requested for payment: {PaymentId}, amount: {Amount}, user: {UserId}", paymentId, request?.Amount ?? payment.Amount, userId);

            var success = await paymentService.RefundPaymentAsync(paymentId, request?.Amount);
            if (success)
            {
                this.logger.LogInformation("Payment refunded successfully: {PaymentId}, amount: {Amount}", paymentId, request?.Amount ?? payment.Amount);
                return Ok(new { message = "Payment refunded successfully" });
            }

            this.logger.LogError("Failed to refund payment: {PaymentId}", paymentId);
            return BadRequest(new { message = "Failed to refund payment" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while refunding payment");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refunding payment");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            // Security metric: Invalid token attempts
            logger.LogWarning("Security: Invalid user ID in token for IP {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

public class RefundPaymentRequest
{
    public decimal? Amount { get; set; }
}
