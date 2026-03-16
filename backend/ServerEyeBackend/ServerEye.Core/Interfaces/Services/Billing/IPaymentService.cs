namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;

public interface IPaymentService
{
    Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(
        Guid userId,
        CreatePaymentIntentRequest request);
    
    Task<List<PaymentDto>> GetUserPaymentsAsync(Guid userId, int limit = 50);
    
    Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId);
    
    Task<bool> RefundPaymentAsync(Guid paymentId, decimal? amount = null);
}
