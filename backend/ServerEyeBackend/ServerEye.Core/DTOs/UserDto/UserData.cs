namespace ServerEye.Core.DTOs.UserDto;

public class UserData
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid ServerId { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Plan limits (nullable for backward compatibility)
    public int? MaxServers { get; set; }
    public int? CurrentServers { get; set; }
    public int? MetricsRetentionDays { get; set; }
    public string? PlanName { get; set; }
    public string? PlanType { get; set; }
    public bool? HasActiveSubscription { get; set; }
}
