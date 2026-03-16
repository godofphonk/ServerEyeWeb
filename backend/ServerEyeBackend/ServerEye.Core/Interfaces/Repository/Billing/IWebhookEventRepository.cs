namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;

public interface IWebhookEventRepository
{
    Task<WebhookEvent?> GetByEventIdAsync(string eventId);
    Task<List<WebhookEvent>> GetUnprocessedAsync(int limit = 100);
    Task AddAsync(WebhookEvent webhookEvent);
    Task UpdateAsync(WebhookEvent webhookEvent);
}
