namespace ServerEye.Core.DTOs.Auth;

public class AccountDeletionResponseDto
{
    public string? Code { get; set; }
    public bool EmailSent { get; set; }
    public DateTime ExpiresAt { get; set; }
}
