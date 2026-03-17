namespace ServerEye.Infrastructure.Repositories.Billing;

using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly ServerEyeDbContext context;

    public SubscriptionPlanRepository(ServerEyeDbContext context) => this.context = context;

    public async Task<SubscriptionPlanEntity?> GetByIdAsync(Guid id)
    {
        return await context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<SubscriptionPlanEntity?> GetByPlanTypeAsync(SubscriptionPlan planType)
    {
        return await context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanType == planType);
    }

    public async Task<List<SubscriptionPlanEntity>> GetAllActiveAsync()
    {
        return await context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PlanType)
            .ToListAsync();
    }

    public async Task AddAsync(SubscriptionPlanEntity plan)
    {
        await context.SubscriptionPlans.AddAsync(plan);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SubscriptionPlanEntity plan)
    {
        context.SubscriptionPlans.Update(plan);
        await context.SaveChangesAsync();
    }
}
