namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.Enums;

public interface IWebhookService
{
    public Task<bool> ProcessWebhookAsync(
        PaymentProvider provider,
        string payload,
        string signature);

    public Task ProcessPendingWebhooksAsync();
}
