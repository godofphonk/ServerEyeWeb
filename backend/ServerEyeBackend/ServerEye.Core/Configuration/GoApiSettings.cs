namespace ServerEye.Core.Configuration;

public class GoApiSettings
{
    public Uri BaseUrl { get; set; } = new Uri("http://localhost:8080");
    public Uri ProductionUrl { get; set; } = new Uri("https://api.servereye.dev");
    public string ApiKey { get; set; } = string.Empty;
    public string ServiceId { get; set; } = "csharp-backend";
    public int TimeoutSeconds { get; set; } = 30;
}
