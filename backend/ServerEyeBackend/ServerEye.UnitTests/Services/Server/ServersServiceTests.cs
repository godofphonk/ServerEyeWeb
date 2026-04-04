#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests
#pragma warning disable CA1873 // Avoid conditional access in logging - acceptable in tests

namespace ServerEye.UnitTests.Services.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services;
using Xunit;
using ServersServiceImpl = ServerEye.Core.Services.ServersService;

public class ServersServiceTests
{
    private readonly Mock<IServerAccessService> mockServerAccessService;
    private readonly Mock<IMockDataProvider> mockDataProvider;
    private readonly Mock<ILogger<ServersServiceImpl>> mockLogger;
    private readonly ServersConfiguration configuration;
    private readonly ServersServiceImpl serversService;

    public ServersServiceTests()
    {
        this.mockServerAccessService = new Mock<IServerAccessService>();
        this.mockDataProvider = new Mock<IMockDataProvider>();
        this.mockLogger = new Mock<ILogger<ServersServiceImpl>>();

        this.configuration = new ServersConfiguration
        {
            EnableDetailedLogging = true,
            MaxServersPerUser = 10,
            MockDataDelayMs = 0
        };

        this.serversService = new ServersServiceImpl(
            this.mockServerAccessService.Object,
            this.mockDataProvider.Object,
            this.configuration,
            this.mockLogger.Object);
    }

    #region GetUserServersAsync Tests

    [Fact]
    public async Task GetUserServersAsync_WithValidUserId_ShouldReturnServers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockServers = new List<ServerResponse>
        {
            new ServerResponse
            {
                Id = Guid.NewGuid(),
                ServerId = "server-1",
                ServerKey = "key-1",
                Hostname = "test-server-1",
                IpAddress = "192.168.1.100",
                OperatingSystem = "Ubuntu 22.04",
                AccessLevel = AccessLevel.Owner,
                AddedAt = DateTime.UtcNow.AddDays(-7),
                LastSeen = DateTime.UtcNow,
                IsActive = true
            },
            new ServerResponse
            {
                Id = Guid.NewGuid(),
                ServerId = "server-2",
                ServerKey = "key-2",
                Hostname = "test-server-2",
                IpAddress = "192.168.1.101",
                OperatingSystem = "Windows Server 2022",
                AccessLevel = AccessLevel.Admin,
                AddedAt = DateTime.UtcNow.AddDays(-3),
                LastSeen = DateTime.UtcNow.AddHours(-2),
                IsActive = false
            }
        };

        this.mockServerAccessService
            .Setup(x => x.GetUserServersAsync(userId))
            .ReturnsAsync(mockServers);

        // Act
        var result = await this.serversService.GetUserServersAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Servers.Should().HaveCount(2);

        var firstServer = result.Servers.ElementAt(0);
        firstServer.Name.Should().Be("test-server-1");
        firstServer.Hostname.Should().Be("test-server-1");
        firstServer.IpAddress.Should().Be("192.168.1.100");
        firstServer.Os.Should().Be("Ubuntu 22.04");
        firstServer.Status.Should().Be("online");
        firstServer.Tags.Should().Contain("Owner");

        var secondServer = result.Servers.ElementAt(1);
        secondServer.Name.Should().Be("test-server-2");
        secondServer.Status.Should().Be("offline");
        secondServer.Tags.Should().Contain("Admin");

        this.mockServerAccessService.Verify(x => x.GetUserServersAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserServersAsync_WithEmptyIpAddress_ShouldUseDefaultIp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockServers = new List<ServerResponse>
        {
            new ServerResponse
            {
                Id = Guid.NewGuid(),
                ServerId = "server-1",
                ServerKey = "key-1",
                Hostname = "test-server",
                IpAddress = string.Empty,
                OperatingSystem = "Linux",
                AccessLevel = AccessLevel.Owner,
                AddedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                IsActive = true
            }
        };

        this.mockServerAccessService
            .Setup(x => x.GetUserServersAsync(userId))
            .ReturnsAsync(mockServers);

        // Act
        var result = await this.serversService.GetUserServersAsync(userId);

        // Assert
        result.Servers.ElementAt(0).IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task GetUserServersAsync_WithNullLastSeen_ShouldUseCurrentTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockServers = new List<ServerResponse>
        {
            new ServerResponse
            {
                Id = Guid.NewGuid(),
                ServerId = "server-1",
                ServerKey = "key-1",
                Hostname = "test-server",
                IpAddress = "192.168.1.1",
                OperatingSystem = "Linux",
                AccessLevel = AccessLevel.Owner,
                AddedAt = DateTime.UtcNow,
                LastSeen = null,
                IsActive = true
            }
        };

        this.mockServerAccessService
            .Setup(x => x.GetUserServersAsync(userId))
            .ReturnsAsync(mockServers);

        // Act
        var result = await this.serversService.GetUserServersAsync(userId);

        // Assert
        result.Servers.ElementAt(0).LastHeartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Servers.ElementAt(0).UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetUserServersAsync_WhenMaxServersReached_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockServers = new List<ServerResponse>();

        for (int i = 0; i < 10; i++)
        {
            mockServers.Add(new ServerResponse
            {
                Id = Guid.NewGuid(),
                ServerId = $"server-{i}",
                ServerKey = $"key-{i}",
                Hostname = $"server-{i}",
                IpAddress = $"192.168.1.{i}",
                OperatingSystem = "Linux",
                AccessLevel = AccessLevel.Owner,
                AddedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                IsActive = true
            });
        }

        this.mockServerAccessService
            .Setup(x => x.GetUserServersAsync(userId))
            .ReturnsAsync(mockServers);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.serversService.GetUserServersAsync(userId));
    }

    [Fact]
    public async Task GetUserServersAsync_WithNoServers_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockServers = new List<ServerResponse>();

        this.mockServerAccessService
            .Setup(x => x.GetUserServersAsync(userId))
            .ReturnsAsync(mockServers);

        // Act
        var result = await this.serversService.GetUserServersAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Servers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserServersAsync_WhenServiceThrows_ShouldLogAndRethrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedException = new Exception("Database connection failed");

        this.mockServerAccessService
            .Setup(x => x.GetUserServersAsync(userId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            async () => await this.serversService.GetUserServersAsync(userId));

        exception.Message.Should().Be("Database connection failed");

        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetUserServersAsync_WithDetailedLogging_ShouldLogInformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockServers = new List<ServerResponse>
        {
            new ServerResponse
            {
                Id = Guid.NewGuid(),
                ServerId = "server-1",
                ServerKey = "key-1",
                Hostname = "test-server",
                IpAddress = "192.168.1.1",
                OperatingSystem = "Linux",
                AccessLevel = AccessLevel.Owner,
                AddedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                IsActive = true
            }
        };

        this.mockServerAccessService
            .Setup(x => x.GetUserServersAsync(userId))
            .ReturnsAsync(mockServers);

        // Act
        await this.serversService.GetUserServersAsync(userId);

        // Assert
        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting servers for user")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully retrieved")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    #endregion

    #region GetMockServersAsync Tests

    [Fact]
    public async Task GetMockServersAsync_ShouldReturnMockData()
    {
        // Arrange
        var mockResponse = new ServersResponseDto
        {
            Servers = new List<ServerDto>
            {
                new ServerDto
                {
                    Id = "mock-server-1",
                    Name = "Mock Server 1",
                    Hostname = "mock-server-1.local",
                    IpAddress = "192.168.1.100",
                    Os = "Ubuntu 22.04",
                    Status = "online",
                    LastHeartbeat = DateTime.UtcNow,
                    Tags = new List<string> { "mock", "test" }.AsReadOnly(),
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow
                }
            }.AsReadOnly()
        };

        this.mockDataProvider
            .Setup(x => x.GetMockServersAsync())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await this.serversService.GetMockServersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Servers.Should().HaveCount(1);
        result.Servers.ElementAt(0).Id.Should().Be("mock-server-1");
        result.Servers.ElementAt(0).Name.Should().Be("Mock Server 1");

        this.mockDataProvider.Verify(x => x.GetMockServersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMockServersAsync_WithDetailedLogging_ShouldLogInformation()
    {
        // Arrange
        var mockResponse = new ServersResponseDto
        {
            Servers = new List<ServerDto>().AsReadOnly()
        };

        this.mockDataProvider
            .Setup(x => x.GetMockServersAsync())
            .ReturnsAsync(mockResponse);

        // Act
        await this.serversService.GetMockServersAsync();

        // Assert
        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Returning mock servers data")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetMockServersAsync_WithDelay_ShouldRespectConfiguration()
    {
        // Arrange
        var configWithDelay = new ServersConfiguration
        {
            EnableDetailedLogging = false,
            MaxServersPerUser = 10,
            MockDataDelayMs = 100
        };

        var serviceWithDelay = new ServersServiceImpl(
            this.mockServerAccessService.Object,
            this.mockDataProvider.Object,
            configWithDelay,
            this.mockLogger.Object);

        var mockResponse = new ServersResponseDto
        {
            Servers = new List<ServerDto>().AsReadOnly()
        };

        this.mockDataProvider
            .Setup(x => x.GetMockServersAsync())
            .ReturnsAsync(mockResponse);

        // Act
        var startTime = DateTime.UtcNow;
        await serviceWithDelay.GetMockServersAsync();
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(100);
    }

    #endregion
}
