namespace ServerEye.Core.Interfaces.Services;

public interface IAuthService
{
    public Task SendVerificationCodeAsync(Guid userId);
    public Task<bool> VerifyEmailAsync(Guid userId, string code);
    public Task RequestPasswordResetAsync(string email);
    public Task<bool> ResetPasswordAsync(string token, string newPassword);
    public Task RequestEmailChangeAsync(Guid userId, string newEmail);
    public Task<bool> ConfirmEmailChangeAsync(Guid userId, string code);
}
