namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;

public interface ISubscriptionService
{
    public Task<SubscriptionDto?> GetUserSubscriptionAsync(Guid userId);

    public Task<CreateSubscriptionResponse> CreateSubscriptionCheckoutAsync(
        Guid userId,
        CreateSubscriptionRequest request);

    public Task<SubscriptionDto> UpdateSubscriptionPlanAsync(
        Guid userId,
        UpdateSubscriptionRequest request);

    public Task CancelSubscriptionAsync(
        Guid userId,
        CancelSubscriptionRequest request);

    public Task<SubscriptionDto> ReactivateSubscriptionAsync(Guid userId);

    public Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync();

    public Task<bool> HasActiveSubscriptionAsync(Guid userId);

    public Task<bool> CanAccessFeatureAsync(Guid userId, string featureName);

    public Task<int> GetMaxServersForUserAsync(Guid userId);

    public Task CreateFreeSubscriptionAsync(Guid userId);
}
