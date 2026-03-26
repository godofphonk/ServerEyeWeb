#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Billing;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServerEye.Core.Interfaces.Repository.Billing;
using Xunit;

[Collection("Integration Tests")]
public class WebhookControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public WebhookControllerTests(TestCollectionFixture fixture)
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
    public async Task ProcessStripeWebhook_WithValidSignature_ShouldProcessSuccessfully()
    {
        // Arrange
        var payload = CreateStripeWebhookPayload("payment_intent.succeeded");
        var signature = GenerateStripeSignature(payload);

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", signature);

        // Act
        var response = await this.client.PostAsync("/api/billing/webhook/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify webhook event was stored
        using var scope = this.factory.Services.CreateScope();
        var webhookRepo = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();
        var events = await webhookRepo.GetUnprocessedAsync();
        events.Should().NotBeEmpty();
        events.Should().Contain(e => e.EventType == "payment_intent.succeeded");
    }

    [Fact]
    public async Task ProcessStripeWebhook_WithoutSignature_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = CreateStripeWebhookPayload("payment_intent.succeeded");
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await this.client.PostAsync("/api/billing/webhook/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Missing signature");
    }

    [Fact]
    public async Task ProcessStripeWebhook_WithInvalidSignature_ShouldReturnUnauthorized()
    {
        // Arrange
        var payload = CreateStripeWebhookPayload("payment_intent.succeeded");
        var invalidSignature = "t=123,v1=invalid_signature";

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", invalidSignature);

        // Act
        var response = await this.client.PostAsync("/api/billing/webhook/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Invalid signature");
    }

    [Fact]
    public async Task ProcessStripeWebhook_DuplicateEvent_ShouldReturnOk()
    {
        // Arrange
        var payload = CreateStripeWebhookPayload("payment_intent.succeeded");
        var signature = GenerateStripeSignature(payload);

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", signature);

        // First request
        await this.client.PostAsync("/api/billing/webhook/stripe", content);

        // Act - Second request with same event
        var response = await this.client.PostAsync("/api/billing/webhook/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessYooKassaWebhook_WithValidPayload_ShouldProcessSuccessfully()
    {
        // Arrange
        var payload = CreateYooKassaWebhookPayload("payment.succeeded");
        var signature = GenerateYooKassaSignature(payload);

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.Add("Content-HMAC", signature);

        // Act
        var response = await this.client.PostAsync("/api/billing/webhook/yookassa", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify webhook event was stored
        using var scope = this.factory.Services.CreateScope();
        var webhookRepo = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();
        var events = await webhookRepo.GetUnprocessedAsync();
        events.Should().NotBeEmpty();
        events.Should().Contain(e => e.Provider == ServerEye.Core.Enums.PaymentProvider.YooKassa);
    }

    [Fact]
    public async Task ProcessYooKassaWebhook_WithoutSignature_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = CreateYooKassaWebhookPayload("payment.succeeded");
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await this.client.PostAsync("/api/billing/webhook/yookassa", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Missing signature");
    }

    [Fact]
    public async Task ProcessStripeWebhook_CheckoutSessionCompleted_ShouldProcessSuccessfully()
    {
        // Arrange
        var payload = CreateStripeWebhookPayload("checkout.session.completed");
        var signature = GenerateStripeSignature(payload);

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", signature);

        // Act
        var response = await this.client.PostAsync("/api/billing/webhook/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify specific event processing
        using var scope = this.factory.Services.CreateScope();
        var webhookRepo = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();
        var events = await webhookRepo.GetUnprocessedAsync();
        events.Should().Contain(e => e.EventType == "checkout.session.completed");
    }

    [Fact]
    public async Task ProcessStripeWebhook_SubscriptionCreated_ShouldProcessSuccessfully()
    {
        // Arrange
        var payload = CreateStripeWebhookPayload("customer.subscription.created");
        var signature = GenerateStripeSignature(payload);

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", signature);

        // Act
        var response = await this.client.PostAsync("/api/billing/webhook/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify subscription event processing
        using var scope = this.factory.Services.CreateScope();
        var webhookRepo = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();
        var events = await webhookRepo.GetUnprocessedAsync();
        events.Should().Contain(e => e.EventType == "customer.subscription.created");
    }

    [Fact]
    public async Task ProcessStripeWebhook_InvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidPayload = "{ invalid json }";
        var signature = GenerateStripeSignature(invalidPayload);

        var content = new StringContent(invalidPayload, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", signature);

        // Act
        var response = await this.client.PostAsync("/api/billing/webhook/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string CreateStripeWebhookPayload(string eventType)
    {
        return $$"""
        {
            "id": "evt_1234567890",
            "object": "event",
            "api_version": "2023-10-16",
            "created": 1234567890,
            "type": "{{eventType}}",
            "data": {
                "object": {
                    "id": "pi_1234567890",
                    "object": "payment_intent",
                    "amount": 2000,
                    "currency": "usd",
                    "status": "succeeded",
                    "metadata": {
                        "user_id": "11111111-1111-1111-1111-111111111111"
                    }
                }
            }
        }
        """;
    }

    private static string CreateYooKassaWebhookPayload(string eventType)
    {
        return $$"""
        {
            "id": "24f84c6f-0001-5000-9000-1f20f3e22f3f",
            "event": "{{eventType}}",
            "object": {
                "id": "24f84c6f-0001-5000-9000-1f20f3e22f3f",
                "status": "succeeded",
                "amount": {
                    "value": "2000.00",
                    "currency": "RUB"
                },
                "metadata": {
                    "user_id": "11111111-1111-1111-1111-111111111111"
                }
            }
        }
        """;
    }

    private static string GenerateStripeSignature(string payload)
    {
        // In real implementation, this would use HMAC-SHA256 with webhook secret
        // For testing, we'll generate a mock signature
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payloadToSign = $"{timestamp}.{payload}";
        var signature = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(payloadToSign)));
        return $"t={timestamp},v1={signature.ToLower()}";
    }

    private static string GenerateYooKassaSignature(string payload)
    {
        // In real implementation, this would use HMAC-SHA256 with YooKassa secret
        // For testing, we'll generate a mock signature
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLower();
    }
}
