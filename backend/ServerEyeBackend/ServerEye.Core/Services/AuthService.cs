namespace ServerEye.Core.Services;

using System.Security.Cryptography;
using System.Text;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IEmailVerificationRepository emailVerificationRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IEmailService emailService,
    IPasswordHasher passwordHasher) : IAuthService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IEmailVerificationRepository emailVerificationRepository = emailVerificationRepository;
    private readonly IPasswordResetTokenRepository passwordResetTokenRepository = passwordResetTokenRepository;
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
        await this.emailService.SendRegistrationEmailAsync(user.UserName, user.Email);

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
        await this.emailService.SendPasswordResetEmailAsync(user.UserName, user.Email, token);
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
        await this.emailService.SendPasswordChangedNotificationAsync(user.UserName, user.Email);

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
        await this.emailService.SendEmailChangedNotificationAsync(user.UserName, oldEmail, user.Email);

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
