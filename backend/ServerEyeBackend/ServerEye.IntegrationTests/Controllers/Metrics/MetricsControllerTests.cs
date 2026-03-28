#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.IntegrationTests.Controllers.Metrics;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ServerEye.Core.DTOs;
using Xunit;

/// <summary>
/// Integration tests for the MetricsController (GET /api/Metrics/{serverId}/latest).
/// This controller does not require authentication and returns test metric data.
/// </summary>
[Collection("Integration Tests")]
public class MetricsControllerTests : IAsyncLifetime
{
    private readonly TestApplicationFactory factory;
    private readonly HttpClient client;

    public MetricsControllerTests(TestCollectionFixture fixture)
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
    public async Task GetLatestMetrics_WithValidServerId_ShouldReturnOk()
    {
        // Arrange
        var serverId = "test-server-123";

        // Act
        var response = await this.client.GetAsync($"/api/metrics/{serverId}/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLatestMetrics_ShouldReturnThreeMetrics()
    {
        // Arrange
        var serverId = Guid.NewGuid().ToString();

        // Act
        var response = await this.client.GetAsync($"/api/metrics/{serverId}/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<MetricDto[]>();
        metrics.Should().NotBeNull();
        metrics!.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetLatestMetrics_ShouldIncludeCpuTemperatureMetric()
    {
        // Arrange
        var serverId = Guid.NewGuid().ToString();

        // Act
        var response = await this.client.GetAsync($"/api/metrics/{serverId}/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<MetricDto[]>();
        metrics.Should().Contain(m => m.Type == "cpu_temperature");

        var cpuMetric = metrics!.First(m => m.Type == "cpu_temperature");
        cpuMetric.Unit.Should().Be("°C");
        cpuMetric.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetLatestMetrics_ShouldIncludeMemoryUsageMetric()
    {
        // Arrange
        var serverId = Guid.NewGuid().ToString();

        // Act
        var response = await this.client.GetAsync($"/api/metrics/{serverId}/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<MetricDto[]>();
        metrics.Should().Contain(m => m.Type == "memory_usage");

        var memoryMetric = metrics!.First(m => m.Type == "memory_usage");
        memoryMetric.Unit.Should().Be("%");
        memoryMetric.Value.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetLatestMetrics_ShouldIncludeDiskUsageMetric()
    {
        // Arrange
        var serverId = Guid.NewGuid().ToString();

        // Act
        var response = await this.client.GetAsync($"/api/metrics/{serverId}/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<MetricDto[]>();
        metrics.Should().Contain(m => m.Type == "disk_usage");

        var diskMetric = metrics!.First(m => m.Type == "disk_usage");
        diskMetric.Unit.Should().Be("%");
        diskMetric.Value.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetLatestMetrics_ShouldReturnMetricsForCorrectServerId()
    {
        // Arrange
        var serverId = $"specific-server-{Guid.NewGuid():N}";

        // Act
        var response = await this.client.GetAsync($"/api/metrics/{serverId}/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<MetricDto[]>();
        metrics.Should().NotBeNull();
        metrics!.Should().AllSatisfy(m => m.ServerId.Should().Be(serverId));
    }

    [Fact]
    public async Task GetLatestMetrics_ShouldReturnRecentTimestamps()
    {
        // Arrange
        var serverId = Guid.NewGuid().ToString();
        var beforeRequest = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var response = await this.client.GetAsync($"/api/metrics/{serverId}/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<MetricDto[]>();
        metrics.Should().NotBeNull();
        metrics!.Should().AllSatisfy(m =>
            m.Timestamp.Should().BeAfter(beforeRequest));
    }

    [Fact]
    public async Task GetLatestMetrics_WithGuidServerId_ShouldReturnOk()
    {
        // Arrange - use a GUID as server ID (common format)
        var serverId = Guid.NewGuid().ToString();

        // Act
        var response = await this.client.GetAsync($"/api/metrics/{serverId}/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLatestMetrics_DifferentServerIds_ShouldReturnDifferentData()
    {
        // Arrange
        var serverId1 = "server-one-" + Guid.NewGuid().ToString("N")[..8];
        var serverId2 = "server-two-" + Guid.NewGuid().ToString("N")[..8];

        // Act
        var response1 = await this.client.GetAsync($"/api/metrics/{serverId1}/latest");
        var response2 = await this.client.GetAsync($"/api/metrics/{serverId2}/latest");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var metrics1 = await response1.Content.ReadFromJsonAsync<MetricDto[]>();
        var metrics2 = await response2.Content.ReadFromJsonAsync<MetricDto[]>();

        metrics1!.Should().AllSatisfy(m => m.ServerId.Should().Be(serverId1));
        metrics2!.Should().AllSatisfy(m => m.ServerId.Should().Be(serverId2));
    }
}
