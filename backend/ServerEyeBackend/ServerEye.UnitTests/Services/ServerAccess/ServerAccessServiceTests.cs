namespace ServerEye.UnitTests.Services.ServerAccess;

using System.Globalization;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Server;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerAccessServiceImpl = ServerEye.Core.Services.ServerAccessService;

public class ServerAccessServiceTests
{
    private readonly Mock<IMonitoredServerRepository> mockServerRepository;
    private readonly Mock<IUserServerAccessRepository> mockAccessRepository;
    private readonly Mock<IUserExternalLoginRepository> mockExternalLoginRepository;
    private readonly Mock<IGoApiClient> mockGoApiClient;
    private readonly Mock<IEncryptionService> mockEncryptionService;
    private readonly ServerAccessServiceImpl serverAccessService;

    public ServerAccessServiceTests()
    {
        this.mockServerRepository = new Mock<IMonitoredServerRepository>();
        this.mockAccessRepository = new Mock<IUserServerAccessRepository>();
        var mockUserRepository1 = new Mock<IUserRepository>();
        this.mockExternalLoginRepository = new Mock<IUserExternalLoginRepository>();
        this.mockGoApiClient = new Mock<IGoApiClient>();
        this.mockEncryptionService = new Mock<IEncryptionService>();
        var mockLogger1 = new Mock<ILogger<ServerAccessServiceImpl>>();

        this.serverAccessService = new ServerAccessServiceImpl(
            this.mockServerRepository.Object,
            this.mockAccessRepository.Object,
            mockUserRepository1.Object,
            this.mockExternalLoginRepository.Object,
            this.mockGoApiClient.Object,
            this.mockEncryptionService.Object,
            mockLogger1.Object);
    }

    [Fact]
    public async Task AddServerAsync_ShouldCallGoApiForNewServer_WhenServerKeyIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serverKey = "test_server_key";
        var serverInfo = new GoApiServerInfo
        {
            ServerId = "srv_123",
            ServerKey = serverKey,
            Hostname = "test-server",
            OperatingSystem = "Ubuntu 22.04",
            AgentVersion = "1.0.0",
            LastSeen = DateTime.UtcNow
        };

        var sourceResponse = new GoApiSourceResponse
        {
            ServerId = "srv_123",
            Source = "Web",
            Message = "Source added successfully"
        };

        var identifiersResponse = new GoApiSourceIdentifiersResponse
        {
            Message = "Identifiers added successfully",
            ServerId = "srv_123",
            Sources = ["Web"],
            Identifiers = new Dictionary<string, List<SourceIdentifierInfo>>
            {
                ["user_id"] = [new SourceIdentifierInfo
                {
                    Id = 1,
                    ServerId = "srv_123",
                    SourceType = "Web",
                    Identifier = userId.ToString(),
                    IdentifierType = "user_id",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }]
            }
        };

        // Mock GetUserTelegramIdAsync to return null (no Telegram OAuth)
        this.mockExternalLoginRepository
            .Setup(x => x.GetByUserIdAndProviderAsync(userId, OAuthProvider.Telegram))
            .ReturnsAsync((UserExternalLogin?)null);

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverKey))
            .ReturnsAsync(serverInfo);

        this.mockGoApiClient
            .Setup(x => x.AddServerSourceByKeyAsync(serverKey, "Web"))
            .ReturnsAsync(sourceResponse);

        this.mockGoApiClient
            .Setup(x => x.AddServerSourceIdentifiersByKeyAsync(serverKey, It.IsAny<GoApiSourceIdentifiersRequest>()))
            .ReturnsAsync(identifiersResponse);

        this.mockServerRepository
            .Setup(x => x.GetByServerIdAsync(serverInfo.ServerId))
            .ReturnsAsync((Server?)null);

        this.mockEncryptionService
            .Setup(x => x.Encrypt(serverKey))
            .Returns("encrypted_key");

        this.mockAccessRepository
            .Setup(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()))
            .Returns(Task.CompletedTask);

        this.mockServerRepository
            .Setup(x => x.AddAsync(It.IsAny<Server>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.serverAccessService.AddServerAsync(userId, serverKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serverInfo.ServerId, result.ServerId);
        Assert.Equal(serverKey, result.ServerKey);
        Assert.Equal(AccessLevel.Owner, result.AccessLevel);

        // Verify Go API calls
        this.mockGoApiClient.Verify(x => x.ValidateServerKeyAsync(serverKey), Times.Once);
        this.mockGoApiClient.Verify(x => x.AddServerSourceByKeyAsync(serverKey, "Web"), Times.Once);
        this.mockGoApiClient.Verify(
            x => x.AddServerSourceIdentifiersByKeyAsync(
                serverKey, 
                It.Is<GoApiSourceIdentifiersRequest>(req =>
                    req.SourceType == "Web" &&
                    req.IdentifierType == "user_id" &&
                    req.Identifiers.Contains(userId.ToString()) &&
                    req.TelegramId == null &&
                    req.Metadata != null &&
                    req.Metadata.ContainsKey("added_at") &&
                    req.Metadata.ContainsKey("source") &&
                    !req.Metadata.ContainsKey("telegram_linked_at"))),
            Times.Once);

        // Verify database operations
        this.mockServerRepository.Verify(x => x.AddAsync(It.IsAny<Server>()), Times.Once);
        this.mockAccessRepository.Verify(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()), Times.Once);
    }

    [Fact]
    public async Task AddServerAsync_ShouldCallGoApi_WhenAddingAccessToExistingServer()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serverKey = "test_server_key";
        var serverInfo = new GoApiServerInfo
        {
            ServerId = "srv_123",
            ServerKey = serverKey,
            Hostname = "test-server",
            OperatingSystem = "Ubuntu 22.04",
            AgentVersion = "1.0.0",
            LastSeen = DateTime.UtcNow
        };

        var existingServer = new Server
        {
            Id = Guid.NewGuid(),
            ServerId = serverInfo.ServerId,
            ServerKey = "encrypted_key",
            Hostname = serverInfo.Hostname,
            OperatingSystem = serverInfo.OperatingSystem,
            AgentVersion = serverInfo.AgentVersion,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastSeen = serverInfo.LastSeen,
            IsActive = true
        };

        var sourceResponse = new GoApiSourceResponse
        {
            ServerId = "srv_123",
            Source = "Web",
            Message = "Source added successfully"
        };

        var identifiersResponse = new GoApiSourceIdentifiersResponse
        {
            Message = "Identifiers added successfully",
            ServerId = "srv_123",
            Sources = ["Web"],
            Identifiers = new Dictionary<string, List<SourceIdentifierInfo>>
            {
                ["user_id"] = [new SourceIdentifierInfo
                {
                    Id = 1,
                    ServerId = "srv_123",
                    SourceType = "Web",
                    Identifier = userId.ToString(),
                    IdentifierType = "user_id",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }]
            }
        };

        // Mock GetUserTelegramIdAsync to return null (no Telegram OAuth)
        this.mockExternalLoginRepository
            .Setup(x => x.GetByUserIdAndProviderAsync(userId, OAuthProvider.Telegram))
            .ReturnsAsync((UserExternalLogin?)null);

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverKey))
            .ReturnsAsync(serverInfo);

        this.mockGoApiClient
            .Setup(x => x.AddServerSourceByKeyAsync(serverKey, "Web"))
            .ReturnsAsync(sourceResponse);

        this.mockGoApiClient
            .Setup(x => x.AddServerSourceIdentifiersByKeyAsync(serverKey, It.IsAny<GoApiSourceIdentifiersRequest>()))
            .ReturnsAsync(identifiersResponse);

        this.mockServerRepository
            .Setup(x => x.GetByServerIdAsync(serverInfo.ServerId))
            .ReturnsAsync(existingServer);

        this.mockAccessRepository
            .Setup(x => x.HasAccessAsync(userId, serverInfo.ServerId))
            .ReturnsAsync(false);

        this.mockEncryptionService
            .Setup(x => x.Decrypt(existingServer.ServerKey))
            .Returns(serverKey);

        this.mockAccessRepository
            .Setup(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.serverAccessService.AddServerAsync(userId, serverKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingServer.Id, result.Id);
        Assert.Equal(serverInfo.ServerId, result.ServerId);
        Assert.Equal(AccessLevel.Viewer, result.AccessLevel);

        // Verify Go API calls
        this.mockGoApiClient.Verify(x => x.ValidateServerKeyAsync(serverKey), Times.Once);
        this.mockGoApiClient.Verify(x => x.AddServerSourceByKeyAsync(serverKey, "Web"), Times.Never); // Should not add source for existing server
        this.mockGoApiClient.Verify(
            x => x.AddServerSourceIdentifiersByKeyAsync(
                serverKey, 
                It.Is<GoApiSourceIdentifiersRequest>(req =>
                    req.SourceType == "Web" &&
                    req.IdentifierType == "user_id" &&
                    req.Identifiers.Contains(userId.ToString()) &&
                    req.TelegramId == null &&
                    req.Metadata != null &&
                    req.Metadata.ContainsKey("added_at") &&
                    req.Metadata.ContainsKey("source") &&
                    !req.Metadata.ContainsKey("telegram_linked_at"))),
            Times.Once);

        // Verify database operations
        this.mockAccessRepository.Verify(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()), Times.Once);
        this.mockServerRepository.Verify(x => x.AddAsync(It.IsAny<Server>()), Times.Never);
    }

    [Fact]
    public async Task AddServerAsync_ShouldIncludeTelegramId_WhenUserHasTelegramOAuth()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serverKey = "test_server_key";
        var telegramId = 123456789L;
        var serverInfo = new GoApiServerInfo
        {
            ServerId = "srv_123",
            ServerKey = serverKey,
            Hostname = "test-server",
            OperatingSystem = "Ubuntu 22.04",
            AgentVersion = "1.0.0",
            LastSeen = DateTime.UtcNow
        };

        var sourceResponse = new GoApiSourceResponse
        {
            ServerId = "srv_123",
            Source = "Web",
            Message = "Source added successfully"
        };

        var identifiersResponse = new GoApiSourceIdentifiersResponse
        {
            Message = "Identifiers added successfully",
            ServerId = "srv_123",
            Sources = ["Web"],
            Identifiers = new Dictionary<string, List<SourceIdentifierInfo>>
            {
                ["user_id"] = [new SourceIdentifierInfo
                {
                    Id = 1,
                    ServerId = "srv_123",
                    SourceType = "Web",
                    Identifier = userId.ToString(),
                    IdentifierType = "user_id",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }]
            }
        };

        // Mock GetUserTelegramIdAsync to return telegram ID
        var telegramLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = OAuthProvider.Telegram,
            ProviderUserId = telegramId.ToString(CultureInfo.InvariantCulture),
            ProviderEmail = "",
            ProviderUsername = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        this.mockExternalLoginRepository
            .Setup(x => x.GetByUserIdAndProviderAsync(userId, OAuthProvider.Telegram))
            .ReturnsAsync(telegramLogin);

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverKey))
            .ReturnsAsync(serverInfo);

        this.mockGoApiClient
            .Setup(x => x.AddServerSourceByKeyAsync(serverKey, "Web"))
            .ReturnsAsync(sourceResponse);

        this.mockGoApiClient
            .Setup(x => x.AddServerSourceIdentifiersByKeyAsync(serverKey, It.IsAny<GoApiSourceIdentifiersRequest>()))
            .ReturnsAsync(identifiersResponse);

        this.mockServerRepository
            .Setup(x => x.GetByServerIdAsync(serverInfo.ServerId))
            .ReturnsAsync((Server?)null);

        this.mockEncryptionService
            .Setup(x => x.Encrypt(serverKey))
            .Returns("encrypted_key");

        this.mockAccessRepository
            .Setup(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()))
            .Returns(Task.CompletedTask);

        this.mockServerRepository
            .Setup(x => x.AddAsync(It.IsAny<Server>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.serverAccessService.AddServerAsync(userId, serverKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serverInfo.ServerId, result.ServerId);

        // Verify Go API calls
        this.mockGoApiClient.Verify(x => x.ValidateServerKeyAsync(serverKey), Times.Once);
        this.mockGoApiClient.Verify(x => x.AddServerSourceByKeyAsync(serverKey, "Web"), Times.Once);
        this.mockGoApiClient.Verify(
            x => x.AddServerSourceIdentifiersByKeyAsync(
                serverKey, 
                It.Is<GoApiSourceIdentifiersRequest>(req =>
                    req.SourceType == "Web" &&
                    req.IdentifierType == "user_id" &&
                    req.Identifiers.Contains(userId.ToString()) &&
                    req.TelegramId == telegramId &&
                    req.Metadata != null &&
                    req.Metadata.ContainsKey("added_at") &&
                    req.Metadata.ContainsKey("source") &&
                    req.Metadata.ContainsKey("telegram_linked_at"))),
            Times.Once);

        // Verify database operations
        this.mockServerRepository.Verify(x => x.AddAsync(It.IsAny<Server>()), Times.Once);
        this.mockAccessRepository.Verify(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()), Times.Once);
    }

    [Fact]
    public async Task AddServerAsync_ShouldContinueWithWarning_WhenGoApiSourceAddFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serverKey = "test_server_key";
        var serverInfo = new GoApiServerInfo
        {
            ServerId = "srv_123",
            ServerKey = serverKey,
            Hostname = "test-server",
            OperatingSystem = "Ubuntu 22.04",
            AgentVersion = "1.0.0",
            LastSeen = DateTime.UtcNow
        };

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverKey))
            .ReturnsAsync(serverInfo);

        // Mock GetUserTelegramIdAsync to return null (no Telegram OAuth)
        this.mockExternalLoginRepository
            .Setup(x => x.GetByUserIdAndProviderAsync(userId, OAuthProvider.Telegram))
            .ReturnsAsync((UserExternalLogin?)null);

        this.mockGoApiClient
            .Setup(x => x.AddServerSourceByKeyAsync(serverKey, "Web"))
            .ReturnsAsync((GoApiSourceResponse?)null);

        this.mockServerRepository
            .Setup(x => x.GetByServerIdAsync(serverInfo.ServerId))
            .ReturnsAsync((Server?)null);

        this.mockEncryptionService
            .Setup(x => x.Encrypt(serverKey))
            .Returns("encrypted_key");

        this.mockAccessRepository
            .Setup(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()))
            .Returns(Task.CompletedTask);

        this.mockServerRepository
            .Setup(x => x.AddAsync(It.IsAny<Server>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.serverAccessService.AddServerAsync(userId, serverKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serverInfo.ServerId, result.ServerId);

        // Verify Go API calls
        this.mockGoApiClient.Verify(x => x.ValidateServerKeyAsync(serverKey), Times.Once);
        this.mockGoApiClient.Verify(x => x.AddServerSourceByKeyAsync(serverKey, "Web"), Times.Once);
        this.mockGoApiClient.Verify(
            x => x.AddServerSourceIdentifiersByKeyAsync(serverKey, It.IsAny<GoApiSourceIdentifiersRequest>()),
            Times.Never); // Should not be called if source add fails

        // Verify database operations still proceed
        this.mockServerRepository.Verify(x => x.AddAsync(It.IsAny<Server>()), Times.Once);
        this.mockAccessRepository.Verify(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()), Times.Once);
    }

    [Fact]
    public async Task AddServerAsync_ShouldThrowException_WhenServerKeyIsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serverKey = "invalid_server_key";

        this.mockGoApiClient
            .Setup(x => x.ValidateServerKeyAsync(serverKey))
            .ReturnsAsync((GoApiServerInfo?)null);

        // Mock GetUserTelegramIdAsync to return null (no Telegram OAuth)
        this.mockExternalLoginRepository
            .Setup(x => x.GetByUserIdAndProviderAsync(userId, OAuthProvider.Telegram))
            .ReturnsAsync((UserExternalLogin?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => this.serverAccessService.AddServerAsync(userId, serverKey));

        Assert.Equal("Invalid server key", exception.Message);

        // Verify Go API calls
        this.mockGoApiClient.Verify(x => x.ValidateServerKeyAsync(serverKey), Times.Once);
        this.mockGoApiClient.Verify(x => x.AddServerSourceByKeyAsync(serverKey, "Web"), Times.Never);
        this.mockGoApiClient.Verify(
            x => x.AddServerSourceIdentifiersByKeyAsync(serverKey, It.IsAny<GoApiSourceIdentifiersRequest>()),
            Times.Never);

        // Verify no database operations
        this.mockServerRepository.Verify(x => x.AddAsync(It.IsAny<Server>()), Times.Never);
        this.mockAccessRepository.Verify(x => x.AddAccessAsync(It.IsAny<UserServerAccess>()), Times.Never);
    }
}
