#pragma warning disable CA2007 // Do not directly await a Task - ConfigureAwait not needed in tests

namespace ServerEye.UnitTests.Services.Auth;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services;
using Xunit;
using AuthServiceImpl = ServerEye.Core.Services.AuthService;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> mockUserRepository;
    private readonly Mock<IEmailVerificationRepository> mockEmailVerificationRepository;
    private readonly Mock<IPasswordResetTokenRepository> mockPasswordResetTokenRepository;
    private readonly Mock<IAccountDeletionRepository> mockAccountDeletionRepository;
    private readonly Mock<IUserExternalLoginRepository> mockExternalLoginRepository;
    private readonly Mock<IEmailService> mockEmailService;
    private readonly Mock<IPasswordHasher> mockPasswordHasher;
    private readonly AuthServiceImpl authService;

    public AuthServiceTests()
    {
        this.mockUserRepository = new Mock<IUserRepository>();
        this.mockEmailVerificationRepository = new Mock<IEmailVerificationRepository>();
        this.mockPasswordResetTokenRepository = new Mock<IPasswordResetTokenRepository>();
        this.mockAccountDeletionRepository = new Mock<IAccountDeletionRepository>();
        this.mockExternalLoginRepository = new Mock<IUserExternalLoginRepository>();
        this.mockEmailService = new Mock<IEmailService>();
        this.mockPasswordHasher = new Mock<IPasswordHasher>();

        this.authService = new AuthServiceImpl(
            this.mockUserRepository.Object,
            this.mockEmailVerificationRepository.Object,
            this.mockPasswordResetTokenRepository.Object,
            this.mockAccountDeletionRepository.Object,
            this.mockExternalLoginRepository.Object,
            this.mockEmailService.Object,
            this.mockPasswordHasher.Object,
            Mock.Of<ILogger<AuthService>>());
    }

    #region SendVerificationCodeAsync Tests

    [Fact]
    public async Task SendVerificationCodeAsync_WithValidUser_ShouldSendCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockEmailVerificationRepository
            .Setup(x => x.AddAsync(It.IsAny<EmailVerification>()))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendEmailVerificationCodeAsync(user.UserName, user.Email, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await this.authService.SendVerificationCodeAsync(userId);

        // Assert
        this.mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        this.mockEmailVerificationRepository.Verify(x => x.InvalidateAllByUserIdAsync(userId, EmailVerificationType.Registration), Times.Once);
        this.mockEmailVerificationRepository.Verify(x => x.AddAsync(It.IsAny<EmailVerification>()), Times.Once);
        this.mockEmailService.Verify(x => x.SendEmailVerificationCodeAsync(user.UserName, user.Email, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendVerificationCodeAsync_WithAlreadyVerifiedEmail_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.authService.SendVerificationCodeAsync(userId));
    }

    [Fact]
    public async Task SendVerificationCodeAsync_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.authService.SendVerificationCodeAsync(userId));
    }

    [Fact]
    public async Task SendVerificationCodeAsync_WithUserWithoutEmail_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = null,
            UserName = "testuser",
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.authService.SendVerificationCodeAsync(userId));
    }

    #endregion

    #region VerifyEmailAsync Tests

    [Fact]
    public async Task VerifyEmailAsync_WithValidCode_ShouldVerifyEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string code = "123456";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var verification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = user.Email,
            Code = code,
            Type = EmailVerificationType.Registration,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockEmailVerificationRepository
            .Setup(x => x.GetByCodeAsync(code, userId))
            .ReturnsAsync(verification);

        this.mockUserRepository
            .Setup(x => x.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendRegistrationEmailAsync(user.UserName, user.Email))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.authService.VerifyEmailAsync(userId, code);

        // Assert
        result.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedAt.Should().NotBeNull();
        verification.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string code = "123456";

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await this.authService.VerifyEmailAsync(userId, code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string code = "123456";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockEmailVerificationRepository
            .Setup(x => x.GetByCodeAsync(code, userId))
            .ReturnsAsync((EmailVerification?)null);

        // Act
        var result = await this.authService.VerifyEmailAsync(userId, code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredCode_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string code = "123456";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var verification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = user.Email,
            Code = code,
            Type = EmailVerificationType.Registration,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockEmailVerificationRepository
            .Setup(x => x.GetByCodeAsync(code, userId))
            .ReturnsAsync(verification);

        // Act
        var result = await this.authService.VerifyEmailAsync(userId, code);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RequestPasswordResetAsync Tests

    [Fact]
    public async Task RequestPasswordResetAsync_WithValidEmail_ShouldSendResetToken()
    {
        // Arrange
        const string email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(user);

        this.mockPasswordResetTokenRepository
            .Setup(x => x.InvalidateAllByUserIdAsync(user.Id))
            .Returns(Task.CompletedTask);

        this.mockPasswordResetTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<PasswordResetToken>()))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(user.UserName, user.Email, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await this.authService.RequestPasswordResetAsync(email);

        // Assert
        this.mockUserRepository.Verify(x => x.GetByEmailAsync(email), Times.Once);
        this.mockPasswordResetTokenRepository.Verify(x => x.InvalidateAllByUserIdAsync(user.Id), Times.Once);
        this.mockPasswordResetTokenRepository.Verify(x => x.AddAsync(It.IsAny<PasswordResetToken>()), Times.Once);
        this.mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(user.UserName, user.Email, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNonExistentEmail_ShouldNotThrow()
    {
        // Arrange
        const string email = "nonexistent@example.com";

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act & Assert - Should not throw
        await this.authService.RequestPasswordResetAsync(email);
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // Arrange
        const string token = "valid-token";
        const string newPassword = "NewPassword123!";
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "hashed-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockPasswordResetTokenRepository
            .Setup(x => x.GetActiveByTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(resetToken);

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockPasswordHasher
            .Setup(x => x.HashPassword(newPassword))
            .Returns("hashed-password");

        this.mockUserRepository
            .Setup(x => x.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendPasswordChangedNotificationAsync(user.UserName, user.Email))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.authService.ResetPasswordAsync(token, newPassword);

        // Assert
        result.Should().BeTrue();
        user.Password.Should().Be("hashed-password");
        resetToken.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        const string token = "invalid-token";
        const string newPassword = "NewPassword123!";

        this.mockPasswordResetTokenRepository
            .Setup(x => x.GetActiveByTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((PasswordResetToken?)null);

        // Act
        var result = await this.authService.ResetPasswordAsync(token, newPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        const string token = "expired-token";
        const string newPassword = "NewPassword123!";
        var userId = Guid.NewGuid();

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "hashed-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockPasswordResetTokenRepository
            .Setup(x => x.GetActiveByTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(resetToken);

        // Act
        var result = await this.authService.ResetPasswordAsync(token, newPassword);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RequestEmailChangeAsync Tests

    [Fact]
    public async Task RequestEmailChangeAsync_WithValidData_ShouldSendConfirmationCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string newEmail = "newemail@example.com";
        var user = new User
        {
            Id = userId,
            Email = "old@example.com",
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(newEmail))
            .ReturnsAsync((User?)null);

        this.mockEmailVerificationRepository
            .Setup(x => x.InvalidateAllByUserIdAsync(userId, EmailVerificationType.EmailChange))
            .Returns(Task.CompletedTask);

        this.mockUserRepository
            .Setup(x => x.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);

        this.mockEmailVerificationRepository
            .Setup(x => x.AddAsync(It.IsAny<EmailVerification>()))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendEmailChangeConfirmationAsync(user.UserName, newEmail, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await this.authService.RequestEmailChangeAsync(userId, newEmail);

        // Assert
        user.PendingEmail.Should().Be(newEmail);
        this.mockUserRepository.Verify(x => x.UpdateUserAsync(user), Times.Once);
        this.mockEmailService.Verify(x => x.SendEmailChangeConfirmationAsync(user.UserName, newEmail, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestEmailChangeAsync_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string newEmail = "newemail@example.com";

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.authService.RequestEmailChangeAsync(userId, newEmail));
    }

    [Fact]
    public async Task RequestEmailChangeAsync_WithAlreadyUsedEmail_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string newEmail = "existing@example.com";
        var user = new User
        {
            Id = userId,
            Email = "old@example.com",
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = newEmail,
            UserName = "existinguser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockUserRepository
            .Setup(x => x.GetByEmailAsync(newEmail))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.authService.RequestEmailChangeAsync(userId, newEmail));
    }

    #endregion

    #region ConfirmEmailChangeAsync Tests

    [Fact]
    public async Task ConfirmEmailChangeAsync_WithValidCode_ShouldChangeEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string code = "123456";
        const string newEmail = "newemail@example.com";
        var user = new User
        {
            Id = userId,
            Email = "old@example.com",
            PendingEmail = newEmail,
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var verification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = newEmail,
            Code = code,
            Type = EmailVerificationType.EmailChange,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockEmailVerificationRepository
            .Setup(x => x.GetByCodeAsync(code, userId))
            .ReturnsAsync(verification);

        this.mockUserRepository
            .Setup(x => x.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendEmailChangedNotificationAsync(user.UserName, "old@example.com", newEmail))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.authService.ConfirmEmailChangeAsync(userId, code);

        // Assert
        result.Should().BeTrue();
        user.Email.Should().Be(newEmail);
        user.PendingEmail.Should().BeNull();
        verification.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WithUserWithoutPendingEmail_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string code = "123456";
        var user = new User
        {
            Id = userId,
            Email = "old@example.com",
            PendingEmail = null,
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await this.authService.ConfirmEmailChangeAsync(userId, code);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RequestAccountDeletionAsync Tests

    [Fact]
    public async Task RequestAccountDeletionAsync_WithPasswordUser_ShouldVerifyPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string password = "ValidPassword123!";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            HasPassword = true,
            Password = "hashed-password",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, user.Password))
            .Returns(true);

        this.mockAccountDeletionRepository
            .Setup(x => x.InvalidateAllByUserIdAsync(userId))
            .Returns(Task.CompletedTask);

        this.mockAccountDeletionRepository
            .Setup(x => x.AddAsync(It.IsAny<AccountDeletion>()))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendAccountDeletionConfirmationAsync(user.UserName, user.Email, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.authService.RequestAccountDeletionAsync(userId, password);

        // Assert
        result.Should().NotBeNull();
        result.EmailSent.Should().BeTrue();
        result.Code.Should().BeNull();
        this.mockPasswordHasher.Verify(x => x.VerifyPassword(password, user.Password), Times.Once);
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_WithOAuthUser_ShouldVerifyExternalLogins()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            HasPassword = false,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var externalLogins = new List<UserExternalLogin>
        {
            new UserExternalLogin { Id = Guid.NewGuid(), UserId = userId, Provider = OAuthProvider.Google, ProviderUserId = "google123" }
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockExternalLoginRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(externalLogins);

        this.mockAccountDeletionRepository
            .Setup(x => x.InvalidateAllByUserIdAsync(userId))
            .Returns(Task.CompletedTask);

        this.mockAccountDeletionRepository
            .Setup(x => x.AddAsync(It.IsAny<AccountDeletion>()))
            .Returns(Task.CompletedTask);

        this.mockEmailService
            .Setup(x => x.SendAccountDeletionConfirmationAsync(user.UserName, user.Email, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.authService.RequestAccountDeletionAsync(userId, null);

        // Assert
        result.Should().NotBeNull();
        result.EmailSent.Should().BeTrue();
        this.mockExternalLoginRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_WithInvalidPassword_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string password = "InvalidPassword";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            HasPassword = true,
            Password = "hashed-password",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, user.Password))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.authService.RequestAccountDeletionAsync(userId, password));
    }

    #endregion

    #region DeleteAccountAsync Tests

    [Fact]
    public async Task DeleteAccountAsync_WithOAuthUserWithoutEmail_ShouldDeleteAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = null,
            UserName = "testuser",
            HasPassword = false,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var externalLogins = new List<UserExternalLogin>
        {
            new UserExternalLogin { Id = Guid.NewGuid(), UserId = userId, Provider = OAuthProvider.Google, ProviderUserId = "google123" }
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockExternalLoginRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(externalLogins);

        this.mockUserRepository
            .Setup(x => x.DeleteAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        await this.authService.DeleteAccountAsync(userId);

        // Assert
        this.mockUserRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithPasswordUser_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            HasPassword = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.authService.DeleteAccountAsync(userId));
    }

    #endregion

    #region ConfirmAccountDeletionAsync Tests

    [Fact]
    public async Task ConfirmAccountDeletionAsync_WithValidCode_ShouldDeleteAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string code = "123456";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var deletion = new AccountDeletion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = user.Email,
            ConfirmationCode = code,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockAccountDeletionRepository
            .Setup(x => x.GetByCodeAsync(code, userId))
            .ReturnsAsync(deletion);

        this.mockUserRepository
            .Setup(x => x.DeleteAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.authService.ConfirmAccountDeletionAsync(userId, code);

        // Assert
        result.Should().BeTrue();
        deletion.IsUsed.Should().BeTrue();
        this.mockUserRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ConfirmAccountDeletionAsync_WithInvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string code = "invalid-code";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        this.mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        this.mockAccountDeletionRepository
            .Setup(x => x.GetByCodeAsync(code, userId))
            .ReturnsAsync((AccountDeletion?)null);

        // Act
        var result = await this.authService.ConfirmAccountDeletionAsync(userId, code);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetAccountDeletionCodeAsync Tests

    [Fact]
    public async Task GetAccountDeletionCodeAsync_WithActiveDeletion_ShouldReturnCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string expectedCode = "123456";
        var deletion = new AccountDeletion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = "test@example.com",
            ConfirmationCode = expectedCode,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        this.mockAccountDeletionRepository
            .Setup(x => x.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(deletion);

        // Act
        var result = await this.authService.GetAccountDeletionCodeAsync(userId);

        // Assert
        result.Should().Be(expectedCode);
    }

    [Fact]
    public async Task GetAccountDeletionCodeAsync_WithNoActiveDeletion_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        this.mockAccountDeletionRepository
            .Setup(x => x.GetActiveByUserIdAsync(userId))
            .ReturnsAsync((AccountDeletion?)null);

        // Act
        var result = await this.authService.GetAccountDeletionCodeAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
