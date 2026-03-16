namespace ServerEye.Infrastructure.Repositories.Billing;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Infrastructure.Data;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly ServerEyeDbContext context;

    public WebhookEventRepository(ServerEyeDbContext context)
    {
        this.context = context;
    }

    public async Task<WebhookEvent?> GetByEventIdAsync(string eventId)
    {
        return await context.WebhookEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.EventId == eventId);
    }

    public async Task<List<WebhookEvent>> GetUnprocessedAsync(int limit = 100)
    {
        return await context.WebhookEvents
            .AsNoTracking()
            .Where(w => !w.IsProcessed && w.ProcessingAttempts < 3)
            .OrderBy(w => w.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAsync(WebhookEvent webhookEvent)
    {
        await context.WebhookEvents.AddAsync(webhookEvent);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WebhookEvent webhookEvent)
    {
        context.WebhookEvents.Update(webhookEvent);
        await context.SaveChangesAsync();
    }
}
