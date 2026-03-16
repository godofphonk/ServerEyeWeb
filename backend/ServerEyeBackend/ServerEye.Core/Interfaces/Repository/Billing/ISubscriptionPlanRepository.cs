namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlanEntity?> GetByIdAsync(Guid id);
    Task<SubscriptionPlanEntity?> GetByPlanTypeAsync(SubscriptionPlan planType);
    Task<List<SubscriptionPlanEntity>> GetAllActiveAsync();
    Task AddAsync(SubscriptionPlanEntity plan);
    Task UpdateAsync(SubscriptionPlanEntity plan);
}
