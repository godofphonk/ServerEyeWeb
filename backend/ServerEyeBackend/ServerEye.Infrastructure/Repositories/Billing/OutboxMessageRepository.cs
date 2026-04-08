namespace ServerEye.Infrastructure.Repositories.Billing;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Infrastructure.Data;

public class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly BillingDbContext context;

    public OutboxMessageRepository(BillingDbContext context) => this.context = context;

    public async Task AddAsync(OutboxMessage message)
    {
        await context.OutboxMessages.AddAsync(message);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int limit = 100)
    {
        return await context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task MarkAsProcessedAsync(Guid id)
    {
        var message = await context.OutboxMessages.FindAsync(id);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Completed;
            message.ProcessedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAsFailedAsync(Guid id, string errorMessage)
    {
        var message = await context.OutboxMessages.FindAsync(id);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Failed;
            message.Error = errorMessage;
            await context.SaveChangesAsync();
        }
    }

    public async Task IncrementRetryCountAsync(Guid id)
    {
        var message = await context.OutboxMessages.FindAsync(id);
        if (message != null)
        {
            message.RetryCount++;
            await context.SaveChangesAsync();
        }
    }
}
