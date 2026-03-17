namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;

public interface IPaymentService
{
    public Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(
        Guid userId,
        CreatePaymentIntentRequest request);

    public Task<List<PaymentDto>> GetUserPaymentsAsync(Guid userId, int limit = 50);

    public Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId);

    public Task<bool> RefundPaymentAsync(Guid paymentId, decimal? amount = null);
}
