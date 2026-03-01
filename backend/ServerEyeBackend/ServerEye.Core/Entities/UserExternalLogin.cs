namespace ServerEye.Core.Entities;

using ServerEye.Core.Enums;

public class UserExternalLogin
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public OAuthProvider Provider { get; set; }
    public string ProviderUserId { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
    public string ProviderUsername { get; set; } = string.Empty;
    public Uri? ProviderAvatarUrl { get; set; }
    public string ProviderData { get; set; } = string.Empty; // JSON with additional data
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    
    // For audit and security
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}
