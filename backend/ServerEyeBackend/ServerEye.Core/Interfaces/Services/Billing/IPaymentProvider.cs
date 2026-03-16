namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;

public interface IPaymentProvider
{
    PaymentProvider ProviderType { get; }

    Task<string> CreateCustomerAsync(Guid userId, string email, string? name = null);

    Task<CreateSubscriptionResponse> CreateCheckoutSessionAsync(
        string customerId,
        SubscriptionPlan planType,
        bool isYearly,
        string successUrl,
        string cancelUrl);

    Task<string> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        DateTime? trialEnd = null);

    Task UpdateSubscriptionAsync(
        string subscriptionId,
        string newPriceId);

    Task CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelImmediately);

    Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(
        string customerId,
        decimal amount,
        string currency,
        Dictionary<string, string>? metadata = null);

    Task<bool> RefundPaymentAsync(
        string paymentId,
        decimal? amount = null);

    Task<object> GetSubscriptionDetailsAsync(string subscriptionId);

    Task<object> GetPaymentDetailsAsync(string paymentId);

    Task<bool> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        string secret);

    Task<(string EventType, object Data)> ParseWebhookEventAsync(string payload);
}
