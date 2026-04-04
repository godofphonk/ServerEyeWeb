namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ServerEye.Core.Interfaces.Services;

[Route("api/[controller]")]
[EnableCors("AllowFrontend")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : BaseApiController
{
    private readonly INotificationService notificationService = notificationService;

    [HttpGet]
    public async Task<ActionResult<List<Core.DTOs.Notification.NotificationDto>>> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        var notifications = await notificationService.GetUserNotificationsAsync(userId, page, pageSize);
        return Success(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        var userId = GetUserId();
        var count = await notificationService.GetUnreadCountAsync(userId);
        return Success(new { count });
    }

    [HttpPost("{id:guid}/mark-read")]
    public async Task<ActionResult> MarkAsRead(Guid id)
    {
        await notificationService.MarkAsReadAsync(id);
        return Success("Notification marked as read");
    }

    [HttpPost("mark-all-read")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        await notificationService.MarkAllAsReadAsync(userId);
        return Success("All notifications marked as read");
    }
}
