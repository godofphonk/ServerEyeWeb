namespace ServerEye.API.Configuration;

public class SecuritySettings
{
    public bool RequireHttps { get; set; } = true;
    public bool EnableTokenBlacklist { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
    public int LockoutThreshold { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; }
}
