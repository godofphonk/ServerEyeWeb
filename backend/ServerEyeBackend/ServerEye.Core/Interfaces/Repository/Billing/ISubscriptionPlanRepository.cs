namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;

public interface ISubscriptionPlanRepository
{
    public Task<SubscriptionPlanEntity?> GetByIdAsync(Guid id);

    public Task<SubscriptionPlanEntity?> GetByPlanTypeAsync(SubscriptionPlan planType);

    public Task<List<SubscriptionPlanEntity>> GetAllActiveAsync();

    public Task AddAsync(SubscriptionPlanEntity plan);

    public Task UpdateAsync(SubscriptionPlanEntity plan);
}
