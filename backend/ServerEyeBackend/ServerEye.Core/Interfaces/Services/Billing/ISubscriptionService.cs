namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities.Billing;

public interface ISubscriptionService
{
    Task<Subscription?> GetUserSubscriptionAsync(Guid userId);
    Task<Subscription> CreateSubscriptionAsync(Guid userId, Guid planId, string? paymentMethodId = null);
    Task<Subscription> UpdateSubscriptionAsync(Guid userId, Guid newPlanId);
    Task CancelSubscriptionAsync(Guid userId, bool immediately = false);
    Task ResumeSubscriptionAsync(Guid userId);
    Task<bool> HasActiveSubscriptionAsync(Guid userId);
    Task<bool> CanAccessFeatureAsync(Guid userId, string feature);
    Task<SubscriptionLimits> GetSubscriptionLimitsAsync(Guid userId);
    Task HandleSubscriptionEventAsync(SubscriptionEventDto eventDto);
}
