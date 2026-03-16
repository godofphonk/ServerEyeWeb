namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId);
    Task<List<Payment>> GetByUserIdAsync(Guid userId, int limit = 50);
    Task<List<Payment>> GetBySubscriptionIdAsync(Guid subscriptionId);
    Task<List<Payment>> GetByStatusAsync(PaymentStatus status);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
}
