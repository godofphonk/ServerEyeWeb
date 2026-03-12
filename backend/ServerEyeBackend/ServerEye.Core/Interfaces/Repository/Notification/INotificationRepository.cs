namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;

public interface INotificationRepository
{
    public Task<Notification?> GetByIdAsync(Guid id);
    public Task<List<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 50);
    public Task<int> GetUnreadCountAsync(Guid userId);
    public Task<Notification> AddAsync(Notification notification);
    public Task<Notification> UpdateAsync(Notification notification);
    public Task MarkAsReadAsync(Guid id);
    public Task MarkAllAsReadAsync(Guid userId);
    public Task DeleteByTicketIdAsync(Guid ticketId);
}
