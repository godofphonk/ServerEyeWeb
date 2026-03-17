namespace ServerEye.Core.Interfaces.Repository.Billing;

using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;

public interface IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id);

    public Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId);

    public Task<List<Payment>> GetByUserIdAsync(Guid userId, int limit = 50);

    public Task<List<Payment>> GetBySubscriptionIdAsync(Guid subscriptionId);

    public Task<List<Payment>> GetByStatusAsync(PaymentStatus status);

    public Task AddAsync(Payment payment);

    public Task UpdateAsync(Payment payment);
}
