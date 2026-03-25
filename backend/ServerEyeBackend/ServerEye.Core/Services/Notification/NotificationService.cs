namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Notification;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public sealed class NotificationService(INotificationRepository notificationRepository, IUserRepository userRepository, ILogger<NotificationService> logger) : INotificationService
{
    private readonly INotificationRepository notificationRepository = notificationRepository;
    private readonly IUserRepository userRepository = userRepository;
    private readonly ILogger<NotificationService> logger = logger;

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        var notifications = await this.notificationRepository.GetByUserIdAsync(userId, page, pageSize);
        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            TicketId = n.TicketId,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId) => await this.notificationRepository.GetUnreadCountAsync(userId);

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        this.logger.LogInformation("Marking notification as read: {NotificationId}", notificationId);
        await this.notificationRepository.MarkAsReadAsync(notificationId);
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        this.logger.LogInformation("Marking all notifications as read for user: {UserId}", userId);
        await this.notificationRepository.MarkAllAsReadAsync(userId);
    }

    public async Task CreateNotificationAsync(Guid userId, NotificationType type, string title, string message, Guid? ticketId = null)
    {
        this.logger.LogInformation("Creating notification for user: {UserId}, type: {Type}, title: {Title}", userId, type, title);
        
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            TicketId = ticketId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await this.notificationRepository.AddAsync(notification);
        
        this.logger.LogInformation("Notification created successfully: {NotificationId}", notification.Id);
    }

    public async Task NotifyAdminsAboutNewTicketAsync(Guid ticketId, string ticketNumber, string subject)
    {
        var admins = await this.userRepository.GetByRolesAsync([UserRole.Admin, UserRole.Support]);

        foreach (var admin in admins)
        {
            await this.CreateNotificationAsync(
                admin.Id,
                NotificationType.TicketCreated,
                $"New Ticket: {ticketNumber}",
                $"A new ticket has been created: {subject}",
                ticketId);
        }
    }

    public async Task NotifyUserAboutNewMessageAsync(Guid userId, Guid ticketId, string ticketNumber, string senderName)
    {
        await this.CreateNotificationAsync(
            userId,
            NotificationType.NewMessage,
            $"New Message in {ticketNumber}",
            $"{senderName} has sent you a new message",
            ticketId);
    }

    public async Task NotifyUserAboutStatusChangeAsync(Guid userId, Guid ticketId, string ticketNumber, string newStatus)
    {
        await this.CreateNotificationAsync(
            userId,
            NotificationType.TicketStatusChanged,
            $"Ticket Status Updated: {ticketNumber}",
            $"Your ticket status has been changed to: {newStatus}",
            ticketId);
    }
}
