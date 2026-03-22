namespace ServerEye.Core.Configuration;

public class GoApiSettings
{
    public Uri BaseUrl { get; init; } = new Uri("http://127.0.0.1:8080");
    public Uri ProductionUrl { get; init; } = new Uri("https://api.servereye.dev");
    public string ApiKey { get; init; } = string.Empty;
    public string ServiceId { get; init; } = "csharp-backend";
    public int TimeoutSeconds { get; init; } = 30;
}
