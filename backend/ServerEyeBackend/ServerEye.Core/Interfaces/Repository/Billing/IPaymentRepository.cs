namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId);
    Task<List<Payment>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<List<Payment>> GetBySubscriptionIdAsync(Guid subscriptionId);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
}
