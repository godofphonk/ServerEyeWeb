namespace ServerEye.API.Configuration;

public class SecuritySettings
{
    public bool RequireHttps { get; set; } = true;
    public bool AllowHttpForLocalOAuth { get; set; }
    public bool ForceSecureCookies { get; set; }
    public bool EnableTokenBlacklist { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
    public int LockoutThreshold { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; }

    // Rate limiting settings
    public int GlobalRateLimitPerMinute { get; set; } = 100;
    public int AuthRateLimitPerMinute { get; set; } = 10;

    // CSP settings
    public string CspDevelopment { get; set; } = "default-src 'self' 'unsafe-inline' 'unsafe-eval' connect-src 'self' ws: wss: http://localhost:* https://localhost:* http://127.0.0.1:* https://127.0.0.1:* script-src 'self' 'unsafe-inline' 'unsafe-eval' style-src 'self' 'unsafe-inline' img-src 'self' data: blob: font-src 'self' data:";
    public string CspProduction { get; set; } = "default-src 'self'; connect-src 'self' https:; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:";
}
