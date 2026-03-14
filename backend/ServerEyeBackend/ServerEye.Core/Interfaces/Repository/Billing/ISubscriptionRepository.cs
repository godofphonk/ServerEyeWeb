namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id);
    Task<Subscription?> GetByUserIdAsync(Guid userId);
    Task<Subscription?> GetByProviderSubscriptionIdAsync(string providerSubscriptionId);
    Task<List<Subscription>> GetActiveSubscriptionsAsync();
    Task<List<Subscription>> GetExpiringSubscriptionsAsync(DateTime before);
    Task AddAsync(Subscription subscription);
    Task UpdateAsync(Subscription subscription);
    Task DeleteAsync(Guid id);
}
