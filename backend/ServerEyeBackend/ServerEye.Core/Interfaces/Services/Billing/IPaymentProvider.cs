namespace ServerEye.Core.Interfaces.Services.Billing;

using System.Diagnostics.CodeAnalysis;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;

public interface IPaymentProvider
{
    public PaymentProvider ProviderType { get; }

    public Task<string> CreateCustomerAsync(Guid userId, string email, string? name = null);

    [SuppressMessage("Design", "CA1054:URI parameters should not be strings", Justification = "Stripe API requires string URLs")]
    public Task<CreateSubscriptionResponse> CreateCheckoutSessionAsync(
        string customerId,
        SubscriptionPlan planType,
        bool isYearly,
        string successUrl,
        string cancelUrl);

    public Task<string> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        DateTime? trialEnd = null);

    public Task UpdateSubscriptionAsync(
        string subscriptionId,
        string newPriceId);

    public Task CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelImmediately);

    public Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(
        string customerId,
        decimal amount,
        string currency,
        Dictionary<string, string>? metadata = null);

    public Task<bool> RefundPaymentAsync(
        string paymentId,
        decimal? amount = null);

    public Task<object> GetSubscriptionDetailsAsync(string subscriptionId);

    public Task<object> GetPaymentDetailsAsync(string paymentId);

    public Task<bool> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        string secret);

    public Task<(string EventType, object Data)> ParseWebhookEventAsync(string payload);
}
