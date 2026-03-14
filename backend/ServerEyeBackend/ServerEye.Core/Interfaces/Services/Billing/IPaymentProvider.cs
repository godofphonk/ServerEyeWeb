namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;

public interface IPaymentProvider
{
    Task<CreateCustomerResult> CreateCustomerAsync(Guid userId, string email, string? name = null);
    Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);
    Task<CreateSubscriptionResult> CreateSubscriptionAsync(CreateSubscriptionRequest request);
    Task<CancelSubscriptionResult> CancelSubscriptionAsync(string subscriptionId, bool immediately = false);
    Task<UpdateSubscriptionResult> UpdateSubscriptionAsync(string subscriptionId, UpdateSubscriptionRequest request);
    Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request);
    Task<AttachPaymentMethodResult> AttachPaymentMethodAsync(string customerId, string paymentMethodId);
    Task<DetachPaymentMethodResult> DetachPaymentMethodAsync(string paymentMethodId);
    Task<SetDefaultPaymentMethodResult> SetDefaultPaymentMethodAsync(string customerId, string paymentMethodId);
    Task<RefundPaymentResult> RefundPaymentAsync(string paymentId, decimal? amount = null);
    Task<WebhookEventResult> HandleWebhookAsync(string payload, string signature);
}
