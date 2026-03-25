#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using ServerEye.Core.DTOs.Billing;
using ServerEye.Core.Enums;
using Xunit;

[Collection("Integration Tests")]
public class SubscriptionControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public SubscriptionControllerTests(TestCollectionFixture fixture)
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
    public async Task GetAvailablePlans_ShouldReturnAllPlans()
    {
        // Act
        var response = await this.client.GetAsync("/api/subscription/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<SubscriptionPlanDto[]>();
        plans.Should().NotBeNull();
        plans.Should().HaveCount(3);
        plans.Should().Contain(p => p.PlanType == SubscriptionPlan.Free);
        plans.Should().Contain(p => p.PlanType == SubscriptionPlan.Pro);
        plans.Should().Contain(p => p.PlanType == SubscriptionPlan.Enterprise);
    }

    [Fact]
    public async Task GetAvailablePlans_FreePlan_ShouldHaveCorrectProperties()
    {
        // Act
        var response = await this.client.GetAsync("/api/subscription/plans");
        var plans = await response.Content.ReadFromJsonAsync<SubscriptionPlanDto[]>();

        // Assert
        var freePlan = plans?.FirstOrDefault(p => p.PlanType == SubscriptionPlan.Free);
        freePlan.Should().NotBeNull();
        freePlan!.Name.Should().Be("Free");
        freePlan.MonthlyPrice.Should().Be(0);
        freePlan.YearlyPrice.Should().Be(0);
        freePlan.MaxServers.Should().Be(1);
        freePlan.HasAlerts.Should().BeFalse();
        freePlan.HasApiAccess.Should().BeFalse();
        freePlan.HasPrioritySupport.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailablePlans_ProPlan_ShouldHaveCorrectProperties()
    {
        // Act
        var response = await this.client.GetAsync("/api/subscription/plans");
        var plans = await response.Content.ReadFromJsonAsync<SubscriptionPlanDto[]>();

        // Assert
        var proPlan = plans?.FirstOrDefault(p => p.PlanType == SubscriptionPlan.Pro);
        proPlan.Should().NotBeNull();
        proPlan!.Name.Should().Be("Pro");
        proPlan.MonthlyPrice.Should().Be(9.99m);
        proPlan.YearlyPrice.Should().Be(99.99m);
        proPlan.MaxServers.Should().Be(10);
        proPlan.HasAlerts.Should().BeTrue();
        proPlan.HasApiAccess.Should().BeTrue();
        proPlan.HasPrioritySupport.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailablePlans_EnterprisePlan_ShouldHaveCorrectProperties()
    {
        // Act
        var response = await this.client.GetAsync("/api/subscription/plans");
        var plans = await response.Content.ReadFromJsonAsync<SubscriptionPlanDto[]>();

        // Assert
        var enterprisePlan = plans?.FirstOrDefault(p => p.PlanType == SubscriptionPlan.Enterprise);
        enterprisePlan.Should().NotBeNull();
        enterprisePlan!.Name.Should().Be("Enterprise");
        enterprisePlan.MonthlyPrice.Should().Be(50);
        enterprisePlan.YearlyPrice.Should().Be(500);
        enterprisePlan.MaxServers.Should().Be(-1); // Unlimited
        enterprisePlan.HasAlerts.Should().BeTrue();
        enterprisePlan.HasApiAccess.Should().BeTrue();
        enterprisePlan.HasPrioritySupport.Should().BeTrue();
    }
}
