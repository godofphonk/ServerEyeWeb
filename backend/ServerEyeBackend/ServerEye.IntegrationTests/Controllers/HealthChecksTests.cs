namespace ServerEye.IntegrationTests.Controllers;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

[Collection("HealthChecks Tests")]
public class HealthChecksTests : IClassFixture<TestApplicationFactory>, IAsyncLifetime
{
    private readonly TestApplicationFactory factory;

    public HealthChecksTests(TestApplicationFactory factory)
    {
        this.factory = factory;
        // Client will be created in each test method after JWT is configured
    }

    public async Task InitializeAsync()
    {
        await this.factory.EnsureDatabaseCreatedAsync();
        await this.factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_ShouldReturnHealthyStatus()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_ShouldIncludeAllChecks()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        var checks = healthReport.GetProperty("checks");

        checks.EnumerateArray().Should().NotBeEmpty();
        checks.EnumerateArray().Should().Contain(c =>
            c.GetProperty("name").GetString() == "postgres-servereye");
        checks.EnumerateArray().Should().Contain(c =>
            c.GetProperty("name").GetString() == "postgres-tickets");
        checks.EnumerateArray().Should().Contain(c =>
            c.GetProperty("name").GetString() == "redis");
    }

    [Fact]
    public async Task Health_ShouldIncludeDuration()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        healthReport.TryGetProperty("totalDuration", out var duration).Should().BeTrue();
        duration.GetDouble().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task HealthLive_ShouldReturnHealthy()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthReady_ShouldReturnHealthy()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthReady_ShouldCheckDatabaseConnectivity()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ShouldReturnJsonContentType()
    {
        using var client = this.factory.CreateClient();
        var response = await client.GetAsync("/health");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthLive_ShouldBeFast()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        using var client = this.factory.CreateClient();
        await client.GetAsync("/health/live");

        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Increased timeout for test environment
    }
}
