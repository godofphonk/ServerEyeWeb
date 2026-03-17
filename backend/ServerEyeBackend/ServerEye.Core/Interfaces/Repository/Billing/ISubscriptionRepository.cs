namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;

public interface ISubscriptionRepository
{
    public Task<Subscription?> GetByIdAsync(Guid id);

    public Task<Subscription?> GetByUserIdAsync(Guid userId);

    public Task<Subscription?> GetByProviderSubscriptionIdAsync(string providerSubscriptionId);

    public Task<List<Subscription>> GetByStatusAsync(SubscriptionStatus status);

    public Task<List<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate);

    public Task AddAsync(Subscription subscription);

    public Task UpdateAsync(Subscription subscription);

    public Task DeleteAsync(Guid id);
}
