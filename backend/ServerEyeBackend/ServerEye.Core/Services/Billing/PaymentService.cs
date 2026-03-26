using System.Diagnostics;
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
        
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("payment.amount", request.Amount.ToString());
        activity?.SetTag("payment.currency", request.Currency);

        // logger.LogInformation(
        //     "Creating payment intent for user {UserId}, amount {Amount} {Currency}",
        //     userId,
        //     request.Amount,
        //     request.Currency);

        var stopwatch = Stopwatch.StartNew();

        try
        {
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

            // stopwatch.Stop();
            // activity?.SetTag("payment.intent_id", response.PaymentIntentId);
            // activity?.SetTag("db.operation_ms", stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Created payment intent {PaymentIntentId} for user {UserId} in {ElapsedMs}ms",
                response.PaymentIntentId,
                userId,
                stopwatch.ElapsedMilliseconds);

            // Business metric: Payment attempt
            logger.LogInformation(
                "Payment attempt: {Amount} {Currency} for user {UserId}, intent {PaymentIntentId}",
                request.Amount,
                request.Currency,
                userId,
                response.PaymentIntentId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            // activity?.SetTag("error", true);
            // activity?.SetTag("error.type", ex.GetType().Name);
            // activity?.SetTag("elapsed_ms", stopwatch.ElapsedMilliseconds);

            logger.LogError(ex, 
                "Failed to create payment intent for user {UserId} in {ElapsedMs}ms: {ErrorType}",
                userId, 
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name);

            // Business metric: Payment failure
            logger.LogWarning(
                "Payment creation failed: {Amount} {Currency} for user {UserId}, reason {ErrorType}",
                request.Amount,
                request.Currency,
                userId,
                ex.GetType().Name);

            throw;
        }
    }

    public async Task<CreateSubscriptionResponse> CreateSubscriptionCheckoutAsync(
        Guid userId,
        CreateSubscriptionRequest request)
    {
        
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("subscription.plan_type", request.PlanType.ToString());
        activity?.SetTag("subscription.yearly", request.IsYearly.ToString());

        logger.LogInformation(
            "Creating subscription checkout for user {UserId}, plan {PlanType}, yearly {IsYearly}",
            userId,
            request.PlanType,
            request.IsYearly);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var user = await userRepository.GetByIdAsync(userId)
                ?? throw new InvalidOperationException("User not found");

            var provider = providerFactory.GetDefaultProvider();

            // For now, always create a new customer ID since we don't store ProviderCustomerId
            var customerId = await provider.CreateCustomerAsync(userId, user.Email ?? string.Empty, user.UserName);

            var successUrl = request.SuccessUrl ?? "https://localhost:3000/dashboard?subscription=success";
            var cancelUrl = request.CancelUrl ?? "https://localhost:3000/pricing?subscription=canceled";

            var response = await provider.CreateCheckoutSessionAsync(
                customerId,
                request.PlanType,
                request.IsYearly,
                successUrl,
                cancelUrl);

            stopwatch.Stop();
            activity?.SetTag("checkout.session_id", response.SessionId);
            activity?.SetTag("operation_ms", stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Created subscription checkout {SessionId} for user {UserId} in {ElapsedMs}ms",
                response.SessionId,
                userId,
                stopwatch.ElapsedMilliseconds);

            // Business metric: Checkout funnel start
            logger.LogInformation(
                "Checkout funnel started: {PlanType} {IsYearly} for user {UserId}, session {SessionId}",
                request.PlanType,
                request.IsYearly ? "yearly" : "monthly",
                userId,
                response.SessionId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);

            logger.LogError(ex, 
                "Failed to create subscription checkout for user {UserId} in {ElapsedMs}ms: {ErrorType}",
                userId,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name);

            // Business metric: Checkout failure
            logger.LogWarning(
                "Checkout creation failed: {PlanType} for user {UserId}, reason {ErrorType}",
                request.PlanType,
                userId,
                ex.GetType().Name);

            throw;
        }
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
        
        activity?.SetTag("payment.id", paymentId.ToString());
        activity?.SetTag("refund.amount", amount?.ToString() ?? "full");

        logger.LogInformation("Refunding payment {PaymentId}, amount {Amount}", paymentId, amount);

        var stopwatch = Stopwatch.StartNew();

        try
        {
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
                var oldStatus = payment.Status;
                payment.Status = amount.HasValue && amount.Value < payment.Amount
                    ? PaymentStatus.PartiallyRefunded
                    : PaymentStatus.Refunded;
                payment.RefundedAmount = amount ?? payment.Amount;
                payment.RefundedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await paymentRepository.UpdateAsync(payment);

                stopwatch.Stop();
                activity?.SetTag("payment.status", payment.Status.ToString());
                activity?.SetTag("operation_ms", stopwatch.ElapsedMilliseconds);

                logger.LogInformation(
                    "Refunded payment {PaymentId} ({OldStatus} -> {NewStatus}), amount {Amount} in {ElapsedMs}ms",
                    paymentId,
                    oldStatus,
                    payment.Status,
                    amount,
                    stopwatch.ElapsedMilliseconds);

                // Business metric: Revenue impact
                logger.LogInformation(
                    "Revenue impact: Refunded {Amount} {Currency} from user {UserId}, payment {PaymentId}",
                    amount ?? payment.Amount,
                    payment.Currency,
                    payment.UserId,
                    paymentId);

                // Business metric: Refund rate tracking
                logger.LogInformation(
                    "Refund processed: {PaymentId} for user {UserId}, amount {Amount}, reason {RefundType}",
                    paymentId,
                    payment.UserId,
                    amount ?? payment.Amount,
                    amount.HasValue && amount.Value < payment.Amount ? "partial" : "full");
            }
            else
            {
                stopwatch.Stop();
                activity?.SetTag("error", true);
                activity?.SetTag("error.type", "provider_refund_failed");

                logger.LogWarning(
                    "Refund failed for payment {PaymentId} in {ElapsedMs}ms",
                    paymentId,
                    stopwatch.ElapsedMilliseconds);
            }

            return success;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);

            logger.LogError(ex, 
                "Failed to refund payment {PaymentId} in {ElapsedMs}ms: {ErrorType}",
                paymentId,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name);

            // Business metric: Refund failure
            logger.LogWarning(
                "Refund failed: {PaymentId}, amount {Amount}, reason {ErrorType}",
                paymentId,
                amount,
                ex.GetType().Name);

            throw;
        }
    }
}
