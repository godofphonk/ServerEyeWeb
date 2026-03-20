namespace ServerEye.API.Controllers.Billing;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Interfaces.Services.Billing;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService subscriptionService;
    private readonly ILogger<SubscriptionController> logger;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionController> logger)
    {
        this.subscriptionService = subscriptionService;
        this.logger = logger;
    }

    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionDto>> GetCurrentSubscription()
    {
        try
        {
            var userId = GetUserId();
            var subscription = await subscriptionService.GetUserSubscriptionAsync(userId);

            if (subscription == null)
            {
                return NotFound(new { message = "No subscription found" });
            }

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting current subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CreateSubscriptionResponse>> CreateCheckoutSession(
        [FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await subscriptionService.CreateSubscriptionCheckoutAsync(userId, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while creating checkout session");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating checkout session");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("plan")]
    public async Task<ActionResult<SubscriptionDto>> UpdatePlan(
        [FromBody] UpdateSubscriptionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var subscription = await subscriptionService.UpdateSubscriptionPlanAsync(userId, request);
            return Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while updating subscription plan");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating subscription plan");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription(
        [FromBody] CancelSubscriptionRequest request)
    {
        try
        {
            var userId = GetUserId();
            await subscriptionService.CancelSubscriptionAsync(userId, request);
            return Ok(new { message = "Subscription canceled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while canceling subscription");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error canceling subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("reactivate")]
    public async Task<ActionResult<SubscriptionDto>> ReactivateSubscription()
    {
        try
        {
            var userId = GetUserId();
            var subscription = await subscriptionService.ReactivateSubscriptionAsync(userId);
            return Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while reactivating subscription");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reactivating subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetPlans()
    {
        try
        {
            var plans = await subscriptionService.GetAvailablePlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting subscription plans");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("limits")]
    public async Task<ActionResult<object>> GetSubscriptionLimits()
    {
        try
        {
            var userId = GetUserId();
            var maxServers = await subscriptionService.GetMaxServersForUserAsync(userId);
            var hasAlerts = await subscriptionService.CanAccessFeatureAsync(userId, "alerts");
            var hasApi = await subscriptionService.CanAccessFeatureAsync(userId, "api");

            return Ok(new
            {
                maxServers,
                hasAlerts,
                hasApi
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting subscription limits");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
