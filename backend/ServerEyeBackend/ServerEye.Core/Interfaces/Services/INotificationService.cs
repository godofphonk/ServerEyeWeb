namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Notification;
using ServerEye.Core.Enums;

public interface INotificationService
{
    public Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 50);
    public Task<int> GetUnreadCountAsync(Guid userId);
    public Task MarkAsReadAsync(Guid notificationId);
    public Task MarkAllAsReadAsync(Guid userId);
    public Task CreateNotificationAsync(Guid userId, NotificationType type, string title, string message, Guid? ticketId = null);
    public Task NotifyAdminsAboutNewTicketAsync(Guid ticketId, string ticketNumber, string subject);
    public Task NotifyUserAboutNewMessageAsync(Guid userId, Guid ticketId, string ticketNumber, string senderName);
    public Task NotifyUserAboutStatusChangeAsync(Guid userId, Guid ticketId, string ticketNumber, string newStatus);
}
