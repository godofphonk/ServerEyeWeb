#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.Billing;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;
using ServerEye.Core.Services.Billing;
using Xunit;

internal class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> mockPaymentRepository;
    private readonly Mock<ISubscriptionRepository> mockSubscriptionRepository;
    private readonly Mock<IUserRepository> mockUserRepository;
    private readonly Mock<IPaymentProviderFactory> mockProviderFactory;
    private readonly Mock<IPaymentProvider> mockProvider;
    private readonly Mock<ILogger<PaymentService>> mockLogger;
    private readonly PaymentService paymentService;

    public PaymentServiceTests()
    {
        this.mockPaymentRepository = new Mock<IPaymentRepository>();
        this.mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        this.mockUserRepository = new Mock<IUserRepository>();
        this.mockProviderFactory = new Mock<IPaymentProviderFactory>();
        this.mockProvider = new Mock<IPaymentProvider>();
        this.mockLogger = new Mock<ILogger<PaymentService>>();

        this.mockProviderFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(this.mockProvider.Object);

        this.mockProvider
            .Setup(x => x.ProviderType)
            .Returns(PaymentProvider.Stripe);

        this.paymentService = new PaymentService(
            this.mockPaymentRepository.Object,
            this.mockSubscriptionRepository.Object,
            this.mockUserRepository.Object,
            this.mockProviderFactory.Object,
            this.mockLogger.Object);
    }

    #region CreatePaymentIntentAsync Tests

    [Fact]
    public async Task CreatePaymentIntentAsync_WithValidRequest_ShouldCreatePaymentIntent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        var request = new CreatePaymentIntentRequest
        {
            Amount = 1999,
            Currency = "usd",
            Metadata = new Dictionary<string, string> { { "key", "value" } }
        };

        var expectedCustomerId = "cus_123456";
        var expectedPaymentIntentId = "pi_123456";
        var expectedClientSecret = "pi_123456_secret_abcdef";

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockProvider
            .Setup(x => x.CreateCustomerAsync(userId, user.Email, user.UserName))
            .ReturnsAsync(expectedCustomerId);

        this.mockProvider
            .Setup(x => x.CreatePaymentIntentAsync(
                expectedCustomerId,
                request.Amount,
                request.Currency,
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new CreatePaymentIntentResponse
            {
                PaymentIntentId = expectedPaymentIntentId,
                ClientSecret = expectedClientSecret
            });

        this.mockPaymentRepository
            .Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.paymentService.CreatePaymentIntentAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.PaymentIntentId.Should().Be(expectedPaymentIntentId);
        result.ClientSecret.Should().Be(expectedClientSecret);

        this.mockPaymentRepository.Verify(
            x => x.AddAsync(It.Is<Payment>(p =>
                p.UserId == userId &&
                p.ProviderPaymentIntentId == expectedPaymentIntentId &&
                p.Amount == request.Amount &&
                p.Currency == request.Currency &&
                p.Status == PaymentStatus.Pending)),
            Times.Once);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest
        {
            Amount = 1999,
            Currency = "usd"
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.paymentService.CreatePaymentIntentAsync(userId, request));
    }

    #endregion

    #region CreateSubscriptionCheckoutAsync Tests

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_WithValidRequest_ShouldCreateCheckoutSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        var request = new CreateSubscriptionRequest
        {
            PlanType = SubscriptionPlan.Pro,
            IsYearly = false,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        var expectedCustomerId = "cus_123456";
        var expectedSessionId = "cs_123456";

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockProvider
            .Setup(x => x.CreateCustomerAsync(userId, user.Email, user.UserName))
            .ReturnsAsync(expectedCustomerId);

        this.mockProvider
            .Setup(x => x.CreateCheckoutSessionAsync(
                expectedCustomerId,
                request.PlanType,
                request.IsYearly,
                request.SuccessUrl,
                request.CancelUrl))
            .ReturnsAsync(new CreateSubscriptionResponse
            {
                SessionId = expectedSessionId,
                SessionUrl = "https://checkout.stripe.com/pay/cs_123456"
            });

        // Act
        var result = await this.paymentService.CreateSubscriptionCheckoutAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(expectedSessionId);
        result.SessionUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateSubscriptionRequest
        {
            PlanType = SubscriptionPlan.Pro,
            IsYearly = false
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.paymentService.CreateSubscriptionCheckoutAsync(userId, request));
    }

    #endregion

    #region GetUserPaymentsAsync Tests

    [Fact]
    public async Task GetUserPaymentsAsync_WithExistingPayments_ShouldReturnPaymentList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var payments = new List<Payment>
        {
            new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = 1999,
                Currency = "usd",
                Status = PaymentStatus.Succeeded,
                ReceiptUrl = "https://receipt.com/1",
                InvoiceUrl = "https://invoice.com/1",
                CreatedAt = DateTime.UtcNow
            },
            new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = 999,
                Currency = "usd",
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        this.mockPaymentRepository
            .Setup(x => x.GetByUserIdAsync(userId, 50))
            .ReturnsAsync(payments);

        // Act
        var result = await this.paymentService.GetUserPaymentsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].UserId.Should().Be(userId);
        result[0].Amount.Should().Be(1999);
        result[1].Amount.Should().Be(999);
    }

    [Fact]
    public async Task GetUserPaymentsAsync_WithNoPayments_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockPaymentRepository
            .Setup(x => x.GetByUserIdAsync(userId, 50))
            .ReturnsAsync(new List<Payment>());

        // Act
        var result = await this.paymentService.GetUserPaymentsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetPaymentByIdAsync Tests

    [Fact]
    public async Task GetPaymentByIdAsync_WithExistingPayment_ShouldReturnPayment()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            UserId = Guid.NewGuid(),
            Amount = 1999,
            Currency = "usd",
            Status = PaymentStatus.Succeeded,
            ReceiptUrl = "https://receipt.com/1",
            CreatedAt = DateTime.UtcNow
        };

        this.mockPaymentRepository
            .Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        // Act
        var result = await this.paymentService.GetPaymentByIdAsync(paymentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(paymentId);
        result.Amount.Should().Be(1999);
        result.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WithNonExistentPayment_ShouldReturnNull()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        this.mockPaymentRepository
            .Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await this.paymentService.GetPaymentByIdAsync(paymentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RefundPaymentAsync Tests

    [Fact]
    public async Task RefundPaymentAsync_WithSucceededPayment_ShouldRefundFully()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            UserId = Guid.NewGuid(),
            Provider = PaymentProvider.Stripe,
            ProviderPaymentId = "ch_123456",
            Amount = 1999,
            Currency = "usd",
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow
        };

        this.mockPaymentRepository
            .Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        this.mockProviderFactory
            .Setup(x => x.GetProvider(PaymentProvider.Stripe))
            .Returns(this.mockProvider.Object);

        this.mockProvider
            .Setup(x => x.RefundPaymentAsync(payment.ProviderPaymentId, null))
            .ReturnsAsync(true);

        this.mockPaymentRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.paymentService.RefundPaymentAsync(paymentId);

        // Assert
        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmount.Should().Be(payment.Amount);
        payment.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefundPaymentAsync_WithPartialAmount_ShouldRefundPartially()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var refundAmount = 999m;
        var payment = new Payment
        {
            Id = paymentId,
            UserId = Guid.NewGuid(),
            Provider = PaymentProvider.Stripe,
            ProviderPaymentId = "ch_123456",
            Amount = 1999,
            Currency = "usd",
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow
        };

        this.mockPaymentRepository
            .Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        this.mockProviderFactory
            .Setup(x => x.GetProvider(PaymentProvider.Stripe))
            .Returns(this.mockProvider.Object);

        this.mockProvider
            .Setup(x => x.RefundPaymentAsync(payment.ProviderPaymentId, refundAmount))
            .ReturnsAsync(true);

        this.mockPaymentRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.paymentService.RefundPaymentAsync(paymentId, refundAmount);

        // Assert
        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmount.Should().Be(refundAmount);
        payment.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefundPaymentAsync_WithNonExistentPayment_ShouldThrowException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        this.mockPaymentRepository
            .Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync((Payment?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.paymentService.RefundPaymentAsync(paymentId));
    }

    [Fact]
    public async Task RefundPaymentAsync_WithNonSucceededPayment_ShouldThrowException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            UserId = Guid.NewGuid(),
            Amount = 1999,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        this.mockPaymentRepository
            .Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.paymentService.RefundPaymentAsync(paymentId));
    }

    #endregion
}
