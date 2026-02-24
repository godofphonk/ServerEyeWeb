namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.Interfaces.Services;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
[Authorize]
public class NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger) : ControllerBase
{
    private readonly INotificationService notificationService = notificationService;
    private readonly ILogger<NotificationsController> logger = logger;

    [HttpGet]
    public async Task<ActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { message = "Invalid user identifier" });
            }

            var notifications = await this.notificationService.GetUserNotificationsAsync(userId, page, pageSize);
            return this.Ok(notifications);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting notifications");
            return this.StatusCode(500, new { message = "Failed to retrieve notifications" });
        }
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult> GetUnreadCount()
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { message = "Invalid user identifier" });
            }

            var count = await this.notificationService.GetUnreadCountAsync(userId);
            return this.Ok(new { count });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting unread count");
            return this.StatusCode(500, new { message = "Failed to retrieve unread count" });
        }
    }

    [HttpPost("{id:guid}/mark-read")]
    public async Task<ActionResult> MarkAsRead(Guid id)
    {
        try
        {
            await this.notificationService.MarkAsReadAsync(id);
            return this.Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error marking notification as read");
            return this.StatusCode(500, new { message = "Failed to mark notification as read" });
        }
    }

    [HttpPost("mark-all-read")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        try
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return this.Unauthorized(new { message = "Invalid user identifier" });
            }

            await this.notificationService.MarkAllAsReadAsync(userId);
            return this.Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error marking all notifications as read");
            return this.StatusCode(500, new { message = "Failed to mark all notifications as read" });
        }
    }
}
