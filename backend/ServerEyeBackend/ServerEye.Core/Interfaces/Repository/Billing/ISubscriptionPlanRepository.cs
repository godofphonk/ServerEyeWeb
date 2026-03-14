namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id);
    Task<SubscriptionPlan?> GetByNameAsync(string name);
    Task<List<SubscriptionPlan>> GetAllActiveAsync();
    Task AddAsync(SubscriptionPlan plan);
    Task UpdateAsync(SubscriptionPlan plan);
    Task DeleteAsync(Guid id);
}
