namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities.Billing;

public interface IPaymentService
{
    Task<Payment> CreatePaymentAsync(CreatePaymentRequest request);
    Task<Payment?> GetPaymentAsync(Guid paymentId);
    Task<List<Payment>> GetUserPaymentsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<Payment> RefundPaymentAsync(Guid paymentId, decimal? amount = null);
    Task HandlePaymentEventAsync(PaymentEventDto eventDto);
}
