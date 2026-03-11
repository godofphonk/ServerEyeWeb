namespace ServerEye.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public class NotificationRepository(TicketDbContext context) : INotificationRepository
{
    private readonly TicketDbContext context = context;

    public async Task<Notification?> GetByIdAsync(Guid id) => await this.context
        .Notifications
        .AsNoTracking()
        .FirstOrDefaultAsync(n => n.Id == id)
        .ConfigureAwait(false);

    public async Task<List<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 50) => await this.context
        .Notifications
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task<int> GetUnreadCountAsync(Guid userId) => await this.context
        .Notifications
        .Where(n => n.UserId == userId && !n.IsRead)
        .CountAsync()
        .ConfigureAwait(false);

    public async Task<Notification> AddAsync(Notification notification)
    {
        await this.context.Notifications.AddAsync(notification).ConfigureAwait(false);
        await this.context.SaveChangesAsync().ConfigureAwait(false);
        return notification;
    }

    public async Task<Notification> UpdateAsync(Notification notification)
    {
        this.context.Notifications.Update(notification);
        await this.context.SaveChangesAsync().ConfigureAwait(false);
        return notification;
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var notification = await this.context.Notifications.FindAsync(id).ConfigureAwait(false);
        if (notification != null)
        {
            notification.IsRead = true;
            await this.context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await this.context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true))
            .ConfigureAwait(false);
    }

    public async Task DeleteByTicketIdAsync(Guid ticketId)
    {
        await this.context.Notifications
            .Where(n => n.TicketId == ticketId)
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);
    }
}
