#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Billing;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServerEye.API.Controllers.Billing;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Interfaces.Repository.Billing;
using Xunit;

[Collection("Integration Tests")]
public class PaymentControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public PaymentControllerTests(TestCollectionFixture fixture)
    {
        this.factory = fixture.Factory;
        this.client = fixture.Client;
    }

    public async Task InitializeAsync()
    {
        await this.factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreatePaymentIntent_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var request = new CreatePaymentIntentRequest
        {
            Amount = 10.00m,
            Currency = "usd"
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/payment/intent", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>();
        result.Should().NotBeNull();
        result!.ClientSecret.Should().NotBeEmpty();
        result.PaymentIntentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatePaymentIntent_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreatePaymentIntentRequest
        {
            Amount = 10.00m,
            Currency = "usd"
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/payment/intent", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePaymentIntent_WithInvalidAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        var request = new CreatePaymentIntentRequest
        {
            Amount = -10.00m,
            Currency = "usd"
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/payment/intent", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPaymentHistory_WithValidUser_ShouldReturnPayments()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a payment first
        var createRequest = new CreatePaymentIntentRequest
        {
            Amount = 25.00m,
            Currency = "usd"
        };
        await this.client.PostAsJsonAsync("/api/payment/intent", createRequest);

        // Act
        var response = await this.client.GetAsync("/api/payment/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<PaymentDto[]>();
        payments.Should().NotBeNull();
        payments!.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetPaymentHistory_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await this.client.GetAsync("/api/payment/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPaymentById_WithValidPayment_ShouldReturnPayment()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create a payment first
        var createRequest = new CreatePaymentIntentRequest
        {
            Amount = 50.00m,
            Currency = "usd"
        };
        var createResponse = await this.client.PostAsJsonAsync("/api/payment/intent", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>();

        // Get payment from database
        using var scope = this.factory.Services.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var payments = await paymentRepository.GetByUserIdAsync(TestUserId, 10);
        var payment = payments.First();

        // Act
        var response = await this.client.GetAsync($"/api/payment/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
        result.Amount.Should().Be(50.00m);
    }

    [Fact]
    public async Task GetPaymentById_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        // Act
        var response = await this.client.GetAsync($"/api/payment/{paymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefundPayment_WithValidPayment_ShouldRefundSuccessfully()
    {
        // Arrange
        var userToken = await this.CreateTestUser();
        this.client.DefaultRequestHeaders.Authorization = new("Bearer", userToken);

        // Create and complete a payment (mock success)
        using var scope = this.factory.Services.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

        var payment = new ServerEye.Core.Entities.Billing.Payment
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Provider = ServerEye.Core.Enums.PaymentProvider.Stripe,
            ProviderPaymentId = "pi_test_123",
            ProviderPaymentIntentId = "pi_test_123",
            Amount = 30.00m,
            Currency = "usd",
            Status = ServerEye.Core.Enums.PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await paymentRepository.AddAsync(payment);

        var refundRequest = new ServerEye.API.Controllers.Billing.RefundPaymentRequest
        {
            Amount = 15.00m
        };

        // Act
        var response = await this.client.PostAsJsonAsync($"/api/payment/{payment.Id}/refund", refundRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("refunded successfully");
    }

    [Fact]
    public async Task RefundPayment_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var refundRequest = new ServerEye.API.Controllers.Billing.RefundPaymentRequest
        {
            Amount = 10.00m
        };

        // Act
        var response = await this.client.PostAsJsonAsync($"/api/payment/{paymentId}/refund", refundRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private async Task<string> CreateTestUser()
    {
        var registerDto = new ServerEye.Core.DTOs.UserDto.UserRegisterDto
        {
            UserName = $"paymenttest_{Guid.NewGuid():N}",
            Email = $"payment_{Guid.NewGuid():N}@example.com",
            Password = "Test123!"
        };

        var response = await this.client.PostAsJsonAsync("/api/users/register", registerDto);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result.GetProperty("token").GetString()!;
    }
}
