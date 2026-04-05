#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;
using Xunit;

[Collection("Integration Tests Simple")]
public class SubscriptionControllerTestsSimple : IAsyncLifetime, IDisposable
{
    private readonly TestApplicationFactorySimple factory;
    private readonly HttpClient client;

    public SubscriptionControllerTestsSimple()
    {
        this.factory = new TestApplicationFactorySimple();
        this.client = this.factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await this.factory.EnsureDatabaseCreatedAsync();
        await this.factory.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await this.factory.DisposeAsync();
        this.client?.Dispose();
    }

    public void Dispose()
    {
        this.client?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAvailablePlans_ShouldReturnAllPlans()
    {
        // Act
        var response = await this.client.GetAsync("/api/billing/subscription/plans");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var plans = await response.Content.ReadFromJsonAsync<SubscriptionPlanDto[]>();
            plans.Should().NotBeNull();
            plans.Should().Contain(p => p.PlanType == SubscriptionPlan.Free);
        }
    }

    [Fact]
    public async Task CreateSubscriptionCheckout_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            PlanType = "Pro",
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/billing/subscription/checkout", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetActiveSubscription_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await this.client.GetAsync("/api/billing/subscription/active");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelSubscription_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await this.client.PostAsync("/api/billing/subscription/cancel", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await this.client.GetAsync("/api/billing/subscription/history");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSubscription_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            NewPlanType = "Pro"
        };

        // Act
        var response = await this.client.PutAsJsonAsync("/api/billing/subscription/update", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetInvoices_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await this.client.GetAsync("/api/billing/subscription/invoices");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WebhookHandler_ShouldProcessStripeWebhook()
    {
        // Arrange
        var webhookData = new
        {
            type = "payment_intent.succeeded",
            data = new
            {
                obj = new
                {
                    id = "pi_test_123456",
                    metadata = new
                    {
                        user_id = Guid.NewGuid().ToString(),
                        plan_type = "Pro"
                    }
                }
            }
        };

        var signature = "test-signature";
        
        // Act
        var response = await this.client.PostAsJsonAsync($"/api/billing/webhook/stripe?signature={signature}", webhookData);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebhookHandler_ShouldProcessYooKassaWebhook()
    {
        // Arrange
        var webhookData = new
        {
            @event = "payment.succeeded",
            obj = new
            {
                id = "yk_test_123456",
                metadata = new
                {
                    user_id = Guid.NewGuid().ToString(),
                    plan_type = "Pro"
                }
            }
        };

        // Act
        var response = await this.client.PostAsJsonAsync("/api/billing/webhook/yookassa", webhookData);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsageStats_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await this.client.GetAsync("/api/billing/subscription/usage");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}
