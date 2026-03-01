namespace ServerEye.Core.Entities;

using ServerEye.Core.Enums;

// user input
public partial class User
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public Guid ServerId { get; set; } = Guid.Empty;

    public bool IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public string? PendingEmail { get; set; }
    
    // OAuth2 properties
    public bool HasPassword { get; set; } = true;
    public List<UserExternalLogin> ExternalLogins { get; set; } = new();
}
