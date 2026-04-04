namespace ServerEye.Core.Services;

using System.Security.Cryptography;
using System.Text;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Helpers;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

public sealed class AuthService(
    IUserRepository userRepository,
    IEmailVerificationRepository emailVerificationRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IAccountDeletionRepository accountDeletionRepository,
    IUserExternalLoginRepository externalLoginRepository,
    IEmailService emailService,
    IPasswordHasher passwordHasher,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IEmailVerificationRepository emailVerificationRepository = emailVerificationRepository;
    private readonly IPasswordResetTokenRepository passwordResetTokenRepository = passwordResetTokenRepository;
    private readonly IAccountDeletionRepository accountDeletionRepository = accountDeletionRepository;
    private readonly IUserExternalLoginRepository externalLoginRepository = externalLoginRepository;
    private readonly IEmailService emailService = emailService;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly ILogger<AuthService> logger = logger;

    public async Task SendVerificationCodeAsync(Guid userId)
    {
        this.logger.LogInformation("Sending verification code to user: {UserId}", userId);

        var user = await this.userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        if (user.IsEmailVerified)
        {
            this.logger.LogWarning("Verification code requested for already verified email: {UserId}", userId);
            throw new InvalidOperationException("Email is already verified.");
        }

        await this.emailVerificationRepository.InvalidateAllByUserIdAsync(userId, EmailVerificationType.Registration);

        if (string.IsNullOrEmpty(user.Email))
        {
            throw new InvalidOperationException("User does not have an email address");
        }

        var code = GenerateVerificationCode();
        var verification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = user.Email,
            Code = code,
            Type = EmailVerificationType.Registration,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };

        await this.emailVerificationRepository.AddAsync(verification);
        await this.emailService.SendEmailVerificationCodeAsync(user.UserName, user.Email, code);

        this.logger.LogInformation("Verification code sent successfully to: {Email}", LogSanitizer.MaskEmail(user.Email));
    }

    public async Task<bool> VerifyEmailAsync(Guid userId, string code)
    {
        this.logger.LogInformation("Email verification attempt for user: {UserId}", userId);

        var user = await this.userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            this.logger.LogWarning("Email verification failed - user not found: {UserId}", userId);
            return false;
        }

        var verification = await this.emailVerificationRepository.GetByCodeAsync(code, userId);
        if (verification == null || verification.IsUsed || verification.ExpiresAt < DateTime.UtcNow)
        {
            this.logger.LogWarning("Email verification failed - invalid or expired code for user: {UserId}", userId);
            return false;
        }

        verification.IsUsed = true;
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;

        await this.userRepository.UpdateUserAsync(user);

        this.logger.LogInformation("Email verified successfully for user: {UserId}, Email: {Email}", userId, LogSanitizer.MaskEmail(user.Email));

        if (!string.IsNullOrEmpty(user.Email))
        {
            await this.emailService.SendRegistrationEmailAsync(user.UserName, user.Email);
        }

        return true;
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        this.logger.LogInformation("Password reset requested for email: {Email}", LogSanitizer.MaskEmail(email));

        var user = await this.userRepository.GetByEmailAsync(email ?? string.Empty);
        if (user == null)
        {
            this.logger.LogWarning("Password reset requested for non-existent email: {Email}", LogSanitizer.MaskEmail(email));
            return;
        }

        await this.passwordResetTokenRepository.InvalidateAllByUserIdAsync(user.Id);

        var token = GenerateResetToken();
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await this.passwordResetTokenRepository.AddAsync(resetToken);

        if (!string.IsNullOrEmpty(user.Email))
        {
            await this.emailService.SendPasswordResetEmailAsync(user.UserName, user.Email, token);
            this.logger.LogInformation("Password reset email sent to: {Email}", LogSanitizer.MaskEmail(user.Email));
        }
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        this.logger.LogInformation("Password reset attempt with token");

        var hashedToken = HashToken(token);
        var resetToken = await this.passwordResetTokenRepository.GetActiveByTokenAsync(hashedToken);

        if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            this.logger.LogWarning("Password reset failed - invalid or expired token");
            return false;
        }

        var user = await this.userRepository.GetByIdAsync(resetToken.UserId);
        if (user == null)
        {
            return false;
        }

        user.Password = this.passwordHasher.HashPassword(newPassword);
        resetToken.IsUsed = true;

        await this.userRepository.UpdateUserAsync(user);

        this.logger.LogInformation("Password reset successful for user: {UserId}", user.Id);

        if (!string.IsNullOrEmpty(user.Email))
        {
            await this.emailService.SendPasswordChangedNotificationAsync(user.UserName, user.Email);
        }

        return true;
    }

    public async Task RequestEmailChangeAsync(Guid userId, string newEmail)
    {
        this.logger.LogInformation("Email change requested for user: {UserId}, new email: {NewEmail}", userId, LogSanitizer.MaskEmail(newEmail));

        var user = await this.userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        var existingUser = await this.userRepository.GetByEmailAsync(newEmail ?? string.Empty);
        if (existingUser != null)
        {
            this.logger.LogWarning("Email change failed - email already in use: {Email}", LogSanitizer.MaskEmail(newEmail));
            throw new InvalidOperationException("Email is already in use.");
        }

        await this.emailVerificationRepository.InvalidateAllByUserIdAsync(userId, EmailVerificationType.EmailChange);

        user.PendingEmail = newEmail ?? string.Empty;
        await this.userRepository.UpdateUserAsync(user);

        var code = GenerateVerificationCode();
        var verification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = newEmail ?? string.Empty,
            Code = code,
            Type = EmailVerificationType.EmailChange,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };

        await this.emailVerificationRepository.AddAsync(verification);
        await this.emailService.SendEmailChangeConfirmationAsync(user.UserName, newEmail ?? string.Empty, code);

        this.logger.LogInformation("Email change confirmation sent to: {NewEmail}", LogSanitizer.MaskEmail(newEmail));
    }

    public async Task<bool> ConfirmEmailChangeAsync(Guid userId, string code)
    {
        this.logger.LogInformation("Email change confirmation attempt for user: {UserId}", userId);

        var user = await this.userRepository.GetByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.PendingEmail))
        {
            this.logger.LogWarning("Email change confirmation failed - no pending email for user: {UserId}", userId);
            return false;
        }

        var verification = await this.emailVerificationRepository.GetByCodeAsync(code, userId);
        if (verification == null || verification.IsUsed || verification.ExpiresAt < DateTime.UtcNow || verification.Type != EmailVerificationType.EmailChange)
        {
            this.logger.LogWarning("Email change confirmation failed - invalid code for user: {UserId}", userId);
            return false;
        }

        var oldEmail = user.Email;
        verification.IsUsed = true;
        user.Email = user.PendingEmail;
        user.PendingEmail = null;
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;

        await this.userRepository.UpdateUserAsync(user);

        this.logger.LogInformation("Email changed successfully for user: {UserId}, from {OldEmail} to {NewEmail}", userId, LogSanitizer.MaskEmail(oldEmail), LogSanitizer.MaskEmail(user.Email));

        if (!string.IsNullOrEmpty(oldEmail) && !string.IsNullOrEmpty(user.Email))
        {
            await this.emailService.SendEmailChangedNotificationAsync(user.UserName, oldEmail, user.Email);
        }

        return true;
    }

    public async Task<AccountDeletionResponseDto> RequestAccountDeletionAsync(Guid userId, string? password)
    {
        this.logger.LogWarning("Account deletion requested for user: {UserId}", userId);

        var user = await this.userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        // Check authentication based on user type
        if (user.HasPassword)
        {
            // For users with password - verify password
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Password is required for users with password.");
            }

            if (!this.passwordHasher.VerifyPassword(password, user.Password))
            {
                throw new InvalidOperationException("Invalid password.");
            }
        }
        else
        {
            // For OAuth users - verify they have at least one external login
            var externalLogins = await this.externalLoginRepository.GetByUserIdAsync(userId);
            if (externalLogins.Count == 0)
            {
                throw new InvalidOperationException("OAuth user without external logins cannot delete account.");
            }
        }

        await this.accountDeletionRepository.InvalidateAllByUserIdAsync(userId);

        var code = GenerateVerificationCode();
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var deletion = new AccountDeletion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = user.Email ?? string.Empty, // Store empty string for users without email
            ConfirmationCode = code,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await this.accountDeletionRepository.AddAsync(deletion);

        var emailSent = false;

        // Send confirmation only if user has email
        if (!string.IsNullOrEmpty(user.Email))
        {
            await this.emailService.SendAccountDeletionConfirmationAsync(user.UserName, user.Email, code);
            emailSent = true;
        }

        // Return response with code for OAuth users without email
        return new AccountDeletionResponseDto
        {
            Code = emailSent ? null : code, // Return code only if email was not sent
            EmailSent = emailSent,
            ExpiresAt = expiresAt
        };
    }

    public async Task DeleteAccountAsync(Guid userId)
    {
        var user = await this.userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        // Verify this is an OAuth user without email
        if (user.HasPassword)
        {
            throw new InvalidOperationException("This method is only for OAuth users without email. Use RequestAccountDeletionAsync for users with passwords or email.");
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            throw new InvalidOperationException("This method is only for OAuth users without email. Use RequestAccountDeletionAsync for users with email.");
        }

        // Verify they have at least one external login
        var externalLogins = await this.externalLoginRepository.GetByUserIdAsync(userId);
        if (externalLogins.Count == 0)
        {
            throw new InvalidOperationException("OAuth user without external logins cannot delete account.");
        }

        // Delete account immediately without code confirmation
        await this.userRepository.DeleteAsync(userId);
    }

    public async Task<string?> GetAccountDeletionCodeAsync(Guid userId)
    {
        var deletion = await this.accountDeletionRepository.GetActiveByUserIdAsync(userId);
        return deletion?.ConfirmationCode;
    }

    public async Task<bool> ConfirmAccountDeletionAsync(Guid userId, string code)
    {
        var user = await this.userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        var deletion = await this.accountDeletionRepository.GetByCodeAsync(code, userId);
        if (deletion == null || deletion.IsUsed || deletion.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        deletion.IsUsed = true;
        await this.userRepository.DeleteAsync(userId);

        return true;
    }

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    private static string GenerateResetToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
