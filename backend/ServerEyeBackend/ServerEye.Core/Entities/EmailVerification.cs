namespace ServerEye.Core.Entities;

using ServerEye.Core.Enums;

public class EmailVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public EmailVerificationType Type { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AttemptCount { get; set; }

    public User User { get; set; } = null!;
}
