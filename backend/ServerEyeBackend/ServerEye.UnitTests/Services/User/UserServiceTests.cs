#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests
#pragma warning disable CS8602 // Dereference of a possibly null reference - handled by assertions

namespace ServerEye.UnitTests.Services.User;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.DTOs;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.DTOs.UserDto;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services;
using Xunit;
using UserServiceImpl = ServerEye.Core.Services.UserService;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> mockUserRepository;
    private readonly Mock<IPasswordHasher> mockPasswordHasher;
    private readonly Mock<IJwtService> mockJwtService;
    private readonly Mock<IRefreshTokenRepository> mockRefreshTokenRepository;
    private readonly Mock<IAuthService> mockAuthService;
    private readonly IConfiguration configuration;
    private readonly UserServiceImpl userService;

    public UserServiceTests()
    {
        this.mockUserRepository = new Mock<IUserRepository>();
        this.mockPasswordHasher = new Mock<IPasswordHasher>();
        this.mockJwtService = new Mock<IJwtService>();
        this.mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        this.mockAuthService = new Mock<IAuthService>();

        // Create real configuration for testing
        var configData = new Dictionary<string, string?>
        {
            ["Authentication:RequireEmailVerification"] = "true"
        };
        this.configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        this.userService = new UserServiceImpl(
            this.mockUserRepository.Object,
            this.mockPasswordHasher.Object,
            this.mockJwtService.Object,
            this.mockRefreshTokenRepository.Object,
            this.mockAuthService.Object,
            this.configuration,
            Mock.Of<ILogger<UserService>>());
    }

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ShouldReturnUserData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            Role = UserRole.User,
            ServerId = Guid.NewGuid(),
            IsEmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await this.userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.UserName.Should().Be("testuser");
        result.Role.Should().Be("USER");
        result.ServerId.Should().Be(user.ServerId);
        result.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await this.userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_WithExistingUser_ShouldReturnUserData()
    {
        // Arrange
        const string email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = "testuser",
            Role = UserRole.Admin,
            ServerId = Guid.NewGuid(),
            IsEmailVerified = false,
            EmailVerifiedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(user);

        // Act
        var result = await this.userService.GetUserByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
        result.UserName.Should().Be("testuser");
        result.Role.Should().Be("ADMIN");
        result.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        const string email = "nonexistent@example.com";

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await this.userService.GetUserByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                Email = "user1@example.com",
                UserName = "user1",
                Role = UserRole.User,
                ServerId = Guid.NewGuid(),
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                UserName = "user2",
                Role = UserRole.Admin,
                ServerId = Guid.NewGuid(),
                IsEmailVerified = false,
                EmailVerifiedAt = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        this.mockUserRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await this.userService.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].UserName.Should().Be("user1");
        result[0].Role.Should().Be("USER");
        result[1].UserName.Should().Be("user2");
        result[1].Role.Should().Be("ADMIN");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        this.mockUserRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<User>());

        // Act
        var result = await this.userService.GetAllUsersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldCreateUserAndReturnTokens()
    {
        // Arrange
        var userRegisterDto = new UserRegisterDto
        {
            Email = "newuser@example.com",
            UserName = "newuser",
            Password = "Password123!"
        };

        const string hashedPassword = "hashed-password";
        const string accessToken = "access-token";
        const string refreshToken = "refresh-token";

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(userRegisterDto.Email))
            .ReturnsAsync((User?)null);

        this.mockPasswordHasher
            .Setup(x => x.HashPassword(userRegisterDto.Password))
            .Returns(hashedPassword);

        this.mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        this.mockJwtService
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns(accessToken);

        this.mockJwtService
            .Setup(x => x.GenerateRefreshToken(It.IsAny<User>()))
            .Returns(refreshToken);

        this.mockRefreshTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        this.mockAuthService
            .Setup(x => x.SendVerificationCodeAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.userService.CreateUserAsync(userRegisterDto);

        // Assert
        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(userRegisterDto.Email);
        result.User.UserName.Should().Be(userRegisterDto.UserName);
        result.Token.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.ExpiresIn.Should().Be(1800);

        this.mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == userRegisterDto.Email && 
            u.UserName == userRegisterDto.UserName && 
            u.Password == hashedPassword)), Times.Once);

        this.mockAuthService.Verify(x => x.SendVerificationCodeAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var userRegisterDto = new UserRegisterDto
        {
            Email = "existing@example.com",
            UserName = "user",
            Password = "Password123!"
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userRegisterDto.Email,
            UserName = "existinguser",
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(userRegisterDto.Email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.userService.CreateUserAsync(userRegisterDto));
    }

    [Fact]
    public async Task CreateUserAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.userService.CreateUserAsync(null!));
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UserUpdateDto
        {
            UserName = "updateduser",
            Email = "updated@example.com",
            Password = "NewPassword123!",
            ServerId = Guid.NewGuid(),
        };

        var existingUser = new User
        {
            Id = userId,
            Email = "old@example.com",
            UserName = "olduser",
            Password = "old-hashed-password",
            ServerId = Guid.NewGuid(),
            IsEmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        string? newHashedPassword = null;

        if (!string.IsNullOrEmpty(updateDto.Password))
        {
            newHashedPassword = this.mockPasswordHasher.Object.HashPassword(updateDto.Password);
        }

        this.mockUserRepository
            .Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.userService.UpdateUserAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be(updateDto.UserName);
        result.Email.Should().Be(updateDto.Email);
        result.ServerId.Should().Be(updateDto.ServerId);

        this.mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u => 
            u.UserName == updateDto.UserName && 
            u.Email == updateDto.Email && 
            u.Password == newHashedPassword && 
            u.ServerId == updateDto.ServerId)), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WithoutPassword_ShouldNotUpdatePassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UserUpdateDto
        {
            UserName = "updateduser",
            Email = "updated@example.com",
            Password = string.Empty,
            ServerId = Guid.NewGuid(),
        };

        var existingUser = new User
        {
            Id = userId,
            Email = "old@example.com",
            UserName = "olduser",
            Password = "old-hashed-password",
            ServerId = Guid.NewGuid(),
            IsEmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        this.mockUserRepository
            .Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.userService.UpdateUserAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be(updateDto.UserName);
        result.Email.Should().Be(updateDto.Email);

        // Password should not be updated
        this.mockPasswordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistingUser_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UserUpdateDto
        {
            UserName = "updateduser",
            Email = "updated@example.com",
            Password = "NewPassword123!",
            ServerId = Guid.NewGuid(),
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await this.userService.UpdateUserAsync(userId, updateDto));
    }

    [Fact]
    public async Task UpdateUserAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.userService.UpdateUserAsync(userId, null!));
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_ShouldCallRepositoryDelete()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockUserRepository
            .Setup(x => x.DeleteAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        await this.userService.DeleteUserAsync(userId);

        // Assert
        this.mockUserRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
    }

    #endregion

    #region CanUserAccessProtectedResourcesAsync Tests

    [Fact]
    public async Task CanUserAccessProtectedResourcesAsync_WithVerifiedUser_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "verified@example.com",
            HasPassword = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Configuration.GetValue is an extension method and cannot be mocked
        // The actual value will be used from the real configuration

        // Act
        var result = await this.userService.CanUserAccessProtectedResourcesAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserAccessProtectedResourcesAsync_WithUnverifiedUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "unverified@example.com",
            HasPassword = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Configuration.GetValue is an extension method and cannot be mocked
        // The actual value will be used from the real configuration

        // Act
        var result = await this.userService.CanUserAccessProtectedResourcesAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanUserAccessProtectedResourcesAsync_WithOAuthUserWithoutEmail_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = null,
            HasPassword = false,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Configuration.GetValue is an extension method and cannot be mocked
        // The actual value will be used from the real configuration

        // Act
        var result = await this.userService.CanUserAccessProtectedResourcesAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserAccessProtectedResourcesAsync_WithEmailVerificationDisabled_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            HasPassword = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Create service with disabled email verification
        var configData = new Dictionary<string, string?>
        {
            ["Authentication:RequireEmailVerification"] = "false"
        };
        var disabledConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        
        var userServiceWithDisabledVerification = new UserServiceImpl(
            this.mockUserRepository.Object,
            this.mockPasswordHasher.Object,
            this.mockJwtService.Object,
            this.mockRefreshTokenRepository.Object,
            this.mockAuthService.Object,
            disabledConfig,
            Mock.Of<ILogger<UserService>>());

        // Act
        var result = await userServiceWithDisabledVerification.CanUserAccessProtectedResourcesAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserAccessProtectedResourcesAsync_WithNonExistingUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await this.userService.CanUserAccessProtectedResourcesAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LoginUserAsync Tests

    [Fact]
    public async Task LoginUserAsync_WithInvalidEmail_ShouldThrowException()
    {
        // Arrange
        var userLoginDto = new UserLoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(userLoginDto.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await this.userService.LoginUserAsync(userLoginDto));
    }

    [Fact]
    public async Task LoginUserAsync_WithInvalidPassword_ShouldThrowException()
    {
        // Arrange
        var userLoginDto = new UserLoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = userLoginDto.Email,
            UserName = "testuser",
            Password = "hashed-password",
            HasPassword = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(userLoginDto.Email))
            .ReturnsAsync(user);

        this.mockPasswordHasher
            .Setup(x => x.VerifyPassword(userLoginDto.Password, user.Password))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await this.userService.LoginUserAsync(userLoginDto));
    }

    [Fact]
    public async Task LoginUserAsync_WithUnverifiedEmail_ShouldThrowException()
    {
        // Arrange
        var userLoginDto = new UserLoginDto
        {
            Email = "unverified@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = userLoginDto.Email,
            UserName = "testuser",
            Password = "hashed-password",
            HasPassword = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(userLoginDto.Email))
            .ReturnsAsync(user);

        this.mockPasswordHasher
            .Setup(x => x.VerifyPassword(userLoginDto.Password, user.Password))
            .Returns(true);

        // Configuration.GetValue is an extension method and cannot be mocked
        // The actual value will be used from the real configuration

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await this.userService.LoginUserAsync(userLoginDto));
    }

    [Fact]
    public async Task LoginUserAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.userService.LoginUserAsync(null!));
    }

    #endregion
}
