namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetUserSubscriptionAsync(Guid userId);
    
    Task<CreateSubscriptionResponse> CreateSubscriptionCheckoutAsync(
        Guid userId,
        CreateSubscriptionRequest request);
    
    Task<SubscriptionDto> UpdateSubscriptionPlanAsync(
        Guid userId,
        UpdateSubscriptionRequest request);
    
    Task CancelSubscriptionAsync(
        Guid userId,
        CancelSubscriptionRequest request);
    
    Task<SubscriptionDto> ReactivateSubscriptionAsync(Guid userId);
    
    Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync();
    
    Task<bool> HasActiveSubscriptionAsync(Guid userId);
    
    Task<bool> CanAccessFeatureAsync(Guid userId, string featureName);
    
    Task<int> GetMaxServersForUserAsync(Guid userId);
}
