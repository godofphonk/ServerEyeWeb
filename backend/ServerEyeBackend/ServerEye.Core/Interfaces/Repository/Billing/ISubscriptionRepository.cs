namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id);
    Task<Subscription?> GetByUserIdAsync(Guid userId);
    Task<Subscription?> GetByProviderSubscriptionIdAsync(string providerSubscriptionId);
    Task<List<Subscription>> GetByStatusAsync(SubscriptionStatus status);
    Task<List<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate);
    Task AddAsync(Subscription subscription);
    Task UpdateAsync(Subscription subscription);
    Task DeleteAsync(Guid id);
}
