namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;

public interface IOutboxMessageRepository
{
    public Task AddAsync(OutboxMessage message);

    public Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int limit = 100);

    public Task MarkAsProcessedAsync(Guid id);

    public Task MarkAsFailedAsync(Guid id, string errorMessage);

    public Task IncrementRetryCountAsync(Guid id);
}
