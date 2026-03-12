#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.Metrics;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using Xunit;
using MonitoredServerEntity = ServerEye.Core.Entities.Server;
using MetricsServiceImpl = ServerEye.Core.Services.MetricsService;

public class MetricsServiceTests
{
    private readonly Mock<IGoApiClient> mockGoApiClient;
    private readonly Mock<IMetricsCacheService> mockCacheService;
    private readonly Mock<IMonitoredServerRepository> mockServerRepository;
    private readonly Mock<IUserServerAccessRepository> mockAccessRepository;
    private readonly Mock<ILogger<MetricsServiceImpl>> mockLogger;
    private readonly MetricsServiceImpl metricsService;

    public MetricsServiceTests()
    {
        this.mockGoApiClient = new Mock<IGoApiClient>();
        this.mockCacheService = new Mock<IMetricsCacheService>();
        this.mockServerRepository = new Mock<IMonitoredServerRepository>();
        this.mockAccessRepository = new Mock<IUserServerAccessRepository>();
        this.mockLogger = new Mock<ILogger<MetricsServiceImpl>>();

        this.metricsService = new MetricsServiceImpl(
            this.mockGoApiClient.Object,
            this.mockCacheService.Object,
            this.mockServerRepository.Object,
            this.mockAccessRepository.Object,
            this.mockLogger.Object);
    }

    #region GetRealtimeMetricsAsync Tests

    [Fact]
    public async Task GetRealtimeMetricsAsync_WithValidAccess_ShouldReturnMetrics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string serverId = "server-123";
        
        var serverInfo = new GoApiServerInfo
        {
            ServerId = serverId,
            Hostname = "test-server"
        };

        var rawResponse = new RawMetricsResponse
        {
            ServerId = serverId,
            ServerName = serverInfo.Hostname,
            DataPoints = new List<GoApiDataPoint>
            {
                new GoApiDataPoint
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-4),
                    CpuAvg = 45.5,
                    CpuMax = 50.0,
                    CpuMin = 40.0,
                    MemoryAvg = 60.0,
                    MemoryMax = 65.0,
                    MemoryMin = 55.0,
                    DiskAvg = 70.0,
                    DiskMax = 75.0
                }
            },
            Status = "success",
            IsCached = true,
            CachedAt = DateTime.UtcNow
        };

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverId))
            .ReturnsAsync(serverInfo);

        this.mockAccessRepository
            .Setup(x => x.HasAccessAsync(userId, serverId))
            .ReturnsAsync(true);

        this.mockCacheService
            .Setup(x => x.CalculateTTL(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(TimeSpan.FromMinutes(1));

        this.mockCacheService
            .Setup(x => x.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<RawMetricsResponse>>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await this.metricsService.GetRealtimeMetricsAsync(userId, serverId);

        // Assert
        result.Should().NotBeNull();
        result.ServerId.Should().Be(serverId);
        result.IsCached.Should().BeTrue();
    }

    [Fact]
    public async Task GetRealtimeMetricsAsync_WithoutAccess_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string serverId = "server-123";

        this.mockAccessRepository
            .Setup(x => x.HasAccessAsync(userId, serverId))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await this.metricsService.GetRealtimeMetricsAsync(userId, serverId));
    }

    [Fact]
    public async Task GetRealtimeMetricsAsync_WithCustomDuration_ShouldUseProvidedDuration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string serverId = "server-123";
        var duration = TimeSpan.FromMinutes(10);
        
        var serverInfo = new GoApiServerInfo
        {
            ServerId = serverId,
            Hostname = "test-server"
        };

        var rawResponse = new RawMetricsResponse
        {
            ServerId = serverId,
            ServerName = serverInfo.Hostname,
            DataPoints = new List<GoApiDataPoint>(),
            Status = "success",
            IsCached = true
        };

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverId))
            .ReturnsAsync(serverInfo);

        this.mockAccessRepository
            .Setup(x => x.HasAccessAsync(userId, serverId))
            .ReturnsAsync(true);

        this.mockCacheService
            .Setup(x => x.CalculateTTL(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(TimeSpan.FromMinutes(1));

        this.mockCacheService
            .Setup(x => x.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<RawMetricsResponse>>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await this.metricsService.GetRealtimeMetricsAsync(userId, serverId, duration);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetDashboardMetricsAsync Tests

    [Fact]
    public async Task GetDashboardMetricsAsync_WithValidAccess_ShouldReturnMetrics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string serverId = "server-123";
        
        var serverInfo = new GoApiServerInfo
        {
            ServerId = serverId,
            Hostname = "dashboard-server"
        };

        var rawResponse = new RawMetricsResponse
        {
            ServerId = serverId,
            ServerName = serverInfo.Hostname,
            DataPoints = new List<GoApiDataPoint>
            {
                new GoApiDataPoint
                {
                    Timestamp = DateTime.UtcNow,
                    CpuAvg = 30.0,
                    CpuMax = 35.0,
                    CpuMin = 25.0,
                    MemoryAvg = 50.0,
                    MemoryMax = 55.0,
                    MemoryMin = 45.0,
                    DiskAvg = 60.0,
                    DiskMax = 65.0
                }
            },
            Status = "success",
            IsCached = true
        };

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverId))
            .ReturnsAsync(serverInfo);

        this.mockAccessRepository
            .Setup(x => x.HasAccessAsync(userId, serverId))
            .ReturnsAsync(true);

        this.mockCacheService
            .Setup(x => x.CalculateTTL(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(TimeSpan.FromMinutes(1));

        this.mockCacheService
            .Setup(x => x.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<RawMetricsResponse>>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await this.metricsService.GetDashboardMetricsAsync(userId, serverId);

        // Assert
        result.Should().NotBeNull();
        result.ServerId.Should().Be(serverId);
        result.DataPoints.Should().HaveCount(1);
        result.Status.Should().Be("success");
    }

    #endregion

    #region GetMetricsAsync (by ID) Tests

    [Fact]
    public async Task GetMetricsAsync_ById_WithValidServerKey_ShouldReturnMetrics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string serverId = "server-key-123";
        var start = DateTime.UtcNow.AddHours(-1);
        var end = DateTime.UtcNow;

        var serverInfo = new GoApiServerInfo
        {
            ServerId = "internal-server-id",
            Hostname = "validated-server"
        };

        var goApiResponse = new GoApiMetricsResponse
        {
            ServerId = serverInfo.ServerId,
            StartTime = start,
            EndTime = end,
            Granularity = "5m",
            DataPoints = new List<GoApiDataPoint>
            {
                new GoApiDataPoint
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-30),
                    CpuAvg = 40.0,
                    CpuMax = 45.0,
                    CpuMin = 35.0,
                    MemoryAvg = 55.0,
                    MemoryMax = 60.0,
                    MemoryMin = 50.0,
                    DiskAvg = 65.0,
                    DiskMax = 70.0
                }
            },
            TotalPoints = 1,
            Status = new GoApiServerStatus { Online = true }
        };

        var rawResponse = new RawMetricsResponse
        {
            ServerId = serverInfo.ServerId,
            ServerName = serverInfo.Hostname,
            StartTime = start,
            EndTime = end,
            Granularity = "5m",
            DataPoints = goApiResponse.DataPoints,
            TotalPoints = 1,
            Status = "success",
            IsCached = true
        };

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverId))
            .ReturnsAsync(serverInfo);

        this.mockAccessRepository
            .Setup(x => x.HasAccessAsync(userId, serverInfo.ServerId))
            .ReturnsAsync(true);

        this.mockCacheService
            .Setup(x => x.CalculateTTL(start, end))
            .Returns(TimeSpan.FromMinutes(5));

        this.mockCacheService
            .Setup(x => x.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<RawMetricsResponse>>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await this.metricsService.GetMetricsAsync(userId, serverId, start, end, "5m");

        // Assert
        result.Should().NotBeNull();
        result.ServerId.Should().Be(serverInfo.ServerId);
        result.ServerName.Should().Be(serverInfo.Hostname);
        result.DataPoints.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMetricsAsync_ById_WithInvalidServerKey_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string serverId = "invalid-key";
        var start = DateTime.UtcNow.AddHours(-1);
        var end = DateTime.UtcNow;

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverId))
            .ReturnsAsync((GoApiServerInfo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await this.metricsService.GetMetricsAsync(userId, serverId, start, end));
    }

    #endregion

    #region GetMetricsByKeyAsync Tests

    [Fact]
    public async Task GetMetricsByKeyAsync_WithValidKey_ShouldReturnMetrics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string serverKey = "valid-server-key";
        var start = DateTime.UtcNow.AddHours(-2);
        var end = DateTime.UtcNow;

        var serverInfo = new GoApiServerInfo
        {
            ServerId = "server-internal-id",
            Hostname = "key-validated-server"
        };

        var rawResponse = new RawMetricsResponse
        {
            ServerId = serverInfo.ServerId,
            ServerName = serverInfo.Hostname,
            StartTime = start,
            EndTime = end,
            DataPoints = new List<GoApiDataPoint>(),
            Status = "success",
            IsCached = true
        };

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverKey))
            .ReturnsAsync(serverInfo);

        this.mockAccessRepository
            .Setup(x => x.HasAccessAsync(userId, serverInfo.ServerId))
            .ReturnsAsync(true);

        this.mockCacheService
            .Setup(x => x.CalculateTTL(start, end))
            .Returns(TimeSpan.FromMinutes(10));

        this.mockCacheService
            .Setup(x => x.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<RawMetricsResponse>>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await this.metricsService.GetMetricsByKeyAsync(userId, serverKey, start, end);

        // Assert
        result.Should().NotBeNull();
        result.ServerId.Should().Be(serverInfo.ServerId);
        result.IsCached.Should().BeTrue();
    }

    #endregion

    #region Cache Integration Tests

    [Fact]
    public async Task GetRealtimeMetricsAsync_ShouldUseCacheCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string serverId = "cached-server";
        
        var serverInfo = new GoApiServerInfo
        {
            ServerId = serverId,
            Hostname = "cache-test-server"
        };

        var rawResponse = new RawMetricsResponse
        {
            ServerId = serverId,
            ServerName = serverInfo.Hostname,
            DataPoints = new List<GoApiDataPoint>(),
            Status = "success",
            IsCached = false
        };

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverId))
            .ReturnsAsync(serverInfo);

        this.mockAccessRepository
            .Setup(x => x.HasAccessAsync(userId, serverId))
            .ReturnsAsync(true);

        this.mockCacheService
            .Setup(x => x.CalculateTTL(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(TimeSpan.FromMinutes(1));

        this.mockCacheService
            .Setup(x => x.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<RawMetricsResponse>>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await this.metricsService.GetRealtimeMetricsAsync(userId, serverId);

        // Assert
        this.mockCacheService.Verify(
            x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<RawMetricsResponse>>>(),
                It.IsAny<TimeSpan>()),
            Times.Once);

        this.mockCacheService.Verify(
            x => x.CalculateTTL(It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Once);
    }

    #endregion
}
