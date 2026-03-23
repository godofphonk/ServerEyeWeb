namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository paymentRepository;
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly IUserRepository userRepository;
    private readonly IPaymentProviderFactory providerFactory;
    private readonly ILogger<PaymentService> logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        IPaymentProviderFactory providerFactory,
        ILogger<PaymentService> logger)
    {
        this.paymentRepository = paymentRepository;
        this.subscriptionRepository = subscriptionRepository;
        this.userRepository = userRepository;
        this.providerFactory = providerFactory;
        this.logger = logger;
    }

    public async Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(
        Guid userId,
        CreatePaymentIntentRequest request)
    {
        logger.LogInformation(
            "Creating payment intent for user {UserId}, amount {Amount}",
            userId,
            request.Amount);

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId);
        var provider = providerFactory.GetDefaultProvider();

        // For now, always create a new customer ID since we don't store ProviderCustomerId
        var customerId = await provider.CreateCustomerAsync(userId, user.Email ?? string.Empty, user.UserName);

        var metadata = request.Metadata ?? new Dictionary<string, string>();
        metadata["user_id"] = userId.ToString();

        var response = await provider.CreatePaymentIntentAsync(
            customerId,
            request.Amount,
            request.Currency,
            metadata);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider.ProviderType,
            ProviderPaymentIntentId = response.PaymentIntentId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = PaymentStatus.Pending,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await paymentRepository.AddAsync(payment);

        logger.LogInformation(
            "Created payment intent {PaymentIntentId} for user {UserId}",
            response.PaymentIntentId,
            userId);

        return response;
    }

    public async Task<List<PaymentDto>> GetUserPaymentsAsync(Guid userId, int limit = 50)
    {
        var payments = await paymentRepository.GetByUserIdAsync(userId, limit);

        return payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Amount = p.Amount,
            Currency = p.Currency,
            Status = p.Status,
            ReceiptUrl = p.ReceiptUrl,
            InvoiceUrl = p.InvoiceUrl,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
        {
            return null;
        }

        return new PaymentDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status,
            ReceiptUrl = payment.ReceiptUrl,
            InvoiceUrl = payment.InvoiceUrl,
            CreatedAt = payment.CreatedAt
        };
    }

    public async Task<bool> RefundPaymentAsync(Guid paymentId, decimal? amount = null)
    {
        logger.LogInformation("Refunding payment {PaymentId}, amount {Amount}", paymentId, amount);

        var payment = await paymentRepository.GetByIdAsync(paymentId)
            ?? throw new InvalidOperationException("Payment not found");

        if (payment.Status != PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("Can only refund succeeded payments");
        }

        var provider = providerFactory.GetProvider(payment.Provider);

        var success = await provider.RefundPaymentAsync(
            payment.ProviderPaymentId!,
            amount);

        if (success)
        {
            payment.Status = amount.HasValue && amount.Value < payment.Amount
                ? PaymentStatus.PartiallyRefunded
                : PaymentStatus.Refunded;
            payment.RefundedAmount = amount ?? payment.Amount;
            payment.RefundedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await paymentRepository.UpdateAsync(payment);

            logger.LogInformation("Refunded payment {PaymentId}, amount {Amount}", paymentId, amount);
        }

        return success;
    }
}
