namespace ServerEye.Core.Services;

using System.Security.Cryptography;
using System.Text;
using ServerEye.Core.DTOs.Auth;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IEmailVerificationRepository emailVerificationRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IAccountDeletionRepository accountDeletionRepository,
    IUserExternalLoginRepository externalLoginRepository,
    IEmailService emailService,
    IPasswordHasher passwordHasher) : IAuthService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IEmailVerificationRepository emailVerificationRepository = emailVerificationRepository;
    private readonly IPasswordResetTokenRepository passwordResetTokenRepository = passwordResetTokenRepository;
    private readonly IAccountDeletionRepository accountDeletionRepository = accountDeletionRepository;
    private readonly IUserExternalLoginRepository externalLoginRepository = externalLoginRepository;
    private readonly IEmailService emailService = emailService;
    private readonly IPasswordHasher passwordHasher = passwordHasher;

    public async Task SendVerificationCodeAsync(Guid userId)
    {
        var user = await this.userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        if (user.IsEmailVerified)
        {
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
    }

    public async Task<bool> VerifyEmailAsync(Guid userId, string code)
    {
        var user = await this.userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var verification = await this.emailVerificationRepository.GetByCodeAsync(code, userId);
        if (verification == null || verification.IsUsed || verification.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        verification.IsUsed = true;
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;

        await this.userRepository.UpdateUserAsync(user);
        
        if (!string.IsNullOrEmpty(user.Email))
        {
            await this.emailService.SendRegistrationEmailAsync(user.UserName, user.Email);
        }

        return true;
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await this.userRepository.GetByEmailAsync(email);
        if (user == null)
        {
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
        }
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var hashedToken = HashToken(token);
        var resetToken = await this.passwordResetTokenRepository.GetActiveByTokenAsync(hashedToken);

        if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
        {
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
        
        if (!string.IsNullOrEmpty(user.Email))
        {
            await this.emailService.SendPasswordChangedNotificationAsync(user.UserName, user.Email);
        }

        return true;
    }

    public async Task RequestEmailChangeAsync(Guid userId, string newEmail)
    {
        var user = await this.userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        var existingUser = await this.userRepository.GetByEmailAsync(newEmail);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email is already in use.");
        }

        await this.emailVerificationRepository.InvalidateAllByUserIdAsync(userId, EmailVerificationType.EmailChange);

        user.PendingEmail = newEmail;
        await this.userRepository.UpdateUserAsync(user);

        var code = GenerateVerificationCode();
        var verification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = newEmail,
            Code = code,
            Type = EmailVerificationType.EmailChange,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };

        await this.emailVerificationRepository.AddAsync(verification);
        await this.emailService.SendEmailChangeConfirmationAsync(user.UserName, newEmail, code);
    }

    public async Task<bool> ConfirmEmailChangeAsync(Guid userId, string code)
    {
        var user = await this.userRepository.GetByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.PendingEmail))
        {
            return false;
        }

        var verification = await this.emailVerificationRepository.GetByCodeAsync(code, userId);
        if (verification == null || verification.IsUsed || verification.ExpiresAt < DateTime.UtcNow || verification.Type != EmailVerificationType.EmailChange)
        {
            return false;
        }

        var oldEmail = user.Email;
        verification.IsUsed = true;
        user.Email = user.PendingEmail;
        user.PendingEmail = null;
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;

        await this.userRepository.UpdateUserAsync(user);
        
        if (!string.IsNullOrEmpty(oldEmail) && !string.IsNullOrEmpty(user.Email))
        {
            await this.emailService.SendEmailChangedNotificationAsync(user.UserName, oldEmail, user.Email);
        }

        return true;
    }

    public async Task<AccountDeletionResponseDto> RequestAccountDeletionAsync(Guid userId, string? password)
    {
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
