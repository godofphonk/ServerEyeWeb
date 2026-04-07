namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;

public interface IWebhookEventRepository
{
    public Task<WebhookEvent?> GetByEventIdAsync(string eventId);

    public Task<List<WebhookEvent>> GetUnprocessedAsync(int limit = 100);

    public Task<List<WebhookEvent>> GetPendingEventsAsync(int limit = 100);

    public Task<List<WebhookEvent>> GetFailedEventsAsync(int maxRetries, TimeSpan threshold);

    public Task AddAsync(WebhookEvent webhookEvent);

    public Task UpdateAsync(WebhookEvent webhookEvent);
}
