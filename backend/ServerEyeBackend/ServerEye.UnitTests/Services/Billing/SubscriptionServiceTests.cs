#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.Billing;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository.Billing;
using ServerEye.Core.Interfaces.Services.Billing;
using ServerEye.Core.Services.Billing;
using Xunit;

public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionRepository> mockSubscriptionRepository;
    private readonly Mock<IPaymentService> mockPaymentService;
    private readonly Mock<ILogger<SubscriptionService>> mockLogger;
    private readonly SubscriptionService subscriptionService;

    public SubscriptionServiceTests()
    {
        this.mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        this.mockPaymentService = new Mock<IPaymentService>();
        this.mockLogger = new Mock<ILogger<SubscriptionService>>();

        this.subscriptionService = new SubscriptionService(
            this.mockSubscriptionRepository.Object,
            this.mockPaymentService.Object,
            this.mockLogger.Object);
    }

    #region GetUserSubscriptionAsync Tests

    [Fact]
    public async Task GetUserSubscriptionAsync_WithExistingSubscription_ShouldReturnSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var proPlanId = new Guid("841bb3db-424c-46e5-a752-04641391c993");
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = proPlanId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await this.subscriptionService.GetUserSubscriptionAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.PlanType.Should().Be(SubscriptionPlan.Pro);
        result.PlanName.Should().Be("Pro");
        result.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetUserSubscriptionAsync_WithNoSubscription_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await this.subscriptionService.GetUserSubscriptionAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSubscriptionAsync_WithFreePlan_ShouldReturnFreePlanDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var freePlanId = new Guid("f5e8c3a1-2b4d-4e6f-8a9c-1d2e3f4a5b6c");
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = freePlanId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddYears(100),
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await this.subscriptionService.GetUserSubscriptionAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.PlanType.Should().Be(SubscriptionPlan.Free);
        result.Amount.Should().Be(0);
    }

    #endregion

    #region CreateSubscriptionCheckoutAsync Tests

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_WithValidRequest_ShouldCreateCheckout()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateSubscriptionRequest
        {
            PlanType = SubscriptionPlan.Pro,
            IsYearly = false,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        var expectedResponse = new CreateSubscriptionResponse
        {
            SessionId = "cs_123456",
            SessionUrl = "https://checkout.stripe.com/pay/cs_123456"
        };

        this.mockPaymentService
            .Setup(x => x.CreateSubscriptionCheckoutAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await this.subscriptionService.CreateSubscriptionCheckoutAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(expectedResponse.SessionId);
        result.SessionUrl.Should().Be(expectedResponse.SessionUrl);
    }

    #endregion

    #region UpdateSubscriptionPlanAsync Tests

    [Fact]
    public async Task UpdateSubscriptionPlanAsync_ShouldThrowNotImplementedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateSubscriptionRequest
        {
            NewPlanType = SubscriptionPlan.Enterprise
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            async () => await this.subscriptionService.UpdateSubscriptionPlanAsync(userId, request));
    }

    #endregion

    #region CancelSubscriptionAsync Tests

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldThrowNotImplementedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CancelSubscriptionRequest
        {
            CancellationReason = "Too expensive"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            async () => await this.subscriptionService.CancelSubscriptionAsync(userId, request));
    }

    #endregion

    #region ReactivateSubscriptionAsync Tests

    [Fact]
    public async Task ReactivateSubscriptionAsync_ShouldThrowNotImplementedException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            async () => await this.subscriptionService.ReactivateSubscriptionAsync(userId));
    }

    #endregion

    #region GetAvailablePlansAsync Tests

    [Fact]
    public async Task GetAvailablePlansAsync_ShouldReturnAllPlans()
    {
        // Act
        var result = await this.subscriptionService.GetAvailablePlansAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.PlanType == SubscriptionPlan.Free);
        result.Should().Contain(p => p.PlanType == SubscriptionPlan.Pro);
        result.Should().Contain(p => p.PlanType == SubscriptionPlan.Enterprise);
    }

    [Fact]
    public async Task GetAvailablePlansAsync_FreePlan_ShouldHaveCorrectDetails()
    {
        // Act
        var result = await this.subscriptionService.GetAvailablePlansAsync();

        // Assert
        var freePlan = result.First(p => p.PlanType == SubscriptionPlan.Free);
        freePlan.Name.Should().Be("Free");
        freePlan.MonthlyPrice.Should().Be(0);
        freePlan.YearlyPrice.Should().Be(0);
        freePlan.MaxServers.Should().Be(1);
        freePlan.HasAlerts.Should().BeFalse();
        freePlan.HasApiAccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailablePlansAsync_ProPlan_ShouldHaveCorrectDetails()
    {
        // Act
        var result = await this.subscriptionService.GetAvailablePlansAsync();

        // Assert
        var proPlan = result.First(p => p.PlanType == SubscriptionPlan.Pro);
        proPlan.Name.Should().Be("Pro");
        proPlan.MonthlyPrice.Should().Be(9.99m);
        proPlan.YearlyPrice.Should().Be(99.99m);
        proPlan.MaxServers.Should().Be(10);
        proPlan.HasAlerts.Should().BeTrue();
        proPlan.HasApiAccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailablePlansAsync_EnterprisePlan_ShouldHaveCorrectDetails()
    {
        // Act
        var result = await this.subscriptionService.GetAvailablePlansAsync();

        // Assert
        var enterprisePlan = result.First(p => p.PlanType == SubscriptionPlan.Enterprise);
        enterprisePlan.Name.Should().Be("Enterprise");
        enterprisePlan.MonthlyPrice.Should().Be(50);
        enterprisePlan.MaxServers.Should().Be(-1);
        enterprisePlan.HasPrioritySupport.Should().BeTrue();
    }

    #endregion

    #region HasActiveSubscriptionAsync Tests

    [Fact]
    public async Task HasActiveSubscriptionAsync_WithActiveSubscription_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await this.subscriptionService.HasActiveSubscriptionAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveSubscriptionAsync_WithCanceledSubscription_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Canceled,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await this.subscriptionService.HasActiveSubscriptionAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasActiveSubscriptionAsync_WithNoSubscription_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await this.subscriptionService.HasActiveSubscriptionAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CanAccessFeatureAsync Tests

    [Fact]
    public async Task CanAccessFeatureAsync_WithNoSubscription_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await this.subscriptionService.CanAccessFeatureAsync(userId, "ALERTS");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessFeatureAsync_WithInactiveSubscription_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Canceled,
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await this.subscriptionService.CanAccessFeatureAsync(userId, "ALERTS");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessFeatureAsync_WithBasicFeature_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await this.subscriptionService.CanAccessFeatureAsync(userId, "BASIC_MONITORING");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetMaxServersForUserAsync Tests

    [Fact]
    public async Task GetMaxServersForUserAsync_WithNoSubscription_ShouldReturn1()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await this.subscriptionService.GetMaxServersForUserAsync(userId);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task GetMaxServersForUserAsync_WithInactiveSubscription_ShouldReturn1()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Canceled,
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await this.subscriptionService.GetMaxServersForUserAsync(userId);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task GetMaxServersForUserAsync_WithActiveSubscription_ShouldReturn1()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(subscription);

        // Act
        var result = await this.subscriptionService.GetMaxServersForUserAsync(userId);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region CreateFreeSubscriptionAsync Tests

    [Fact]
    public async Task CreateFreeSubscriptionAsync_WithNewUser_ShouldCreateSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((Subscription?)null);

        this.mockSubscriptionRepository
            .Setup(x => x.AddAsync(It.IsAny<Subscription>()))
            .Returns(Task.CompletedTask);

        // Act
        await this.subscriptionService.CreateFreeSubscriptionAsync(userId);

        // Assert
        this.mockSubscriptionRepository.Verify(
            x => x.AddAsync(It.Is<Subscription>(s =>
                s.UserId == userId &&
                s.Status == SubscriptionStatus.Active &&
                s.CancelAtPeriodEnd == false)),
            Times.Once);
    }

    [Fact]
    public async Task CreateFreeSubscriptionAsync_WithExistingSubscription_ShouldNotCreateDuplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        this.mockSubscriptionRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(existingSubscription);

        // Act
        await this.subscriptionService.CreateFreeSubscriptionAsync(userId);

        // Assert
        this.mockSubscriptionRepository.Verify(
            x => x.AddAsync(It.IsAny<Subscription>()),
            Times.Never);
    }

    #endregion
}
