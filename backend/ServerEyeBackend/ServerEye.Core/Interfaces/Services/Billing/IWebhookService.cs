namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.Enums;

public interface IWebhookService
{
    Task<bool> ProcessWebhookAsync(
        PaymentProvider provider,
        string payload,
        string signature);
    
    Task ProcessPendingWebhooksAsync();
}
