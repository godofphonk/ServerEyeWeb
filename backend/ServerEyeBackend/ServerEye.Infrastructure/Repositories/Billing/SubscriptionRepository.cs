namespace ServerEye.Infrastructure.Repositories.Billing;

using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Infrastructure.Data;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly BillingDbContext context;

    public SubscriptionRepository(BillingDbContext context) => this.context = context;

    public async Task<Subscription?> GetByIdAsync(Guid id)
    {
        return await context.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subscription?> GetByUserIdAsync(Guid userId)
    {
        return await context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Subscription>> GetByStatusAsync(SubscriptionStatus status)
    {
        return await context.Subscriptions
            .AsNoTracking()
            .Where(s => s.Status == status)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate)
    {
        return await context.Subscriptions
            .AsNoTracking()
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.CurrentPeriodEnd.HasValue
                && s.CurrentPeriodEnd.Value <= beforeDate)
            .ToListAsync();
    }

    public async Task AddAsync(Subscription subscription)
    {
        await context.Subscriptions.AddAsync(subscription);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Subscription subscription)
    {
        context.Subscriptions.Update(subscription);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var subscription = await context.Subscriptions.FindAsync(id);
        if (subscription != null)
        {
            context.Subscriptions.Remove(subscription);
            await context.SaveChangesAsync();
        }
    }
}
