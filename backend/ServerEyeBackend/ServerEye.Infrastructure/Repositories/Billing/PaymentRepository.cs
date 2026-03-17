namespace ServerEye.Infrastructure.Repositories.Billing;

using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;

public class PaymentRepository : IPaymentRepository
{
    private readonly ServerEyeDbContext context;

    public PaymentRepository(ServerEyeDbContext context) => this.context = context;

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId)
    {
        return await context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId);
    }

    public async Task<List<Payment>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        return await context.Payments
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await context.Payments
            .AsNoTracking()
            .Where(p => p.SubscriptionId == subscriptionId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetByStatusAsync(PaymentStatus status)
    {
        return await context.Payments
            .AsNoTracking()
            .Where(p => p.Status == status)
            .ToListAsync();
    }

    public async Task AddAsync(Payment payment)
    {
        await context.Payments.AddAsync(payment);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        context.Payments.Update(payment);
        await context.SaveChangesAsync();
    }
}
