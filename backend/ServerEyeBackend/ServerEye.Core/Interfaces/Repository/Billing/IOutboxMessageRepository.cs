namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;

public interface IOutboxMessageRepository
{
    Task AddAsync(OutboxMessage message);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int limit = 100);
    Task MarkAsProcessedAsync(Guid id);
    Task MarkAsFailedAsync(Guid id, string error);
    Task IncrementRetryCountAsync(Guid id);
}
