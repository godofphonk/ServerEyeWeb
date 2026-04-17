using System.ComponentModel.DataAnnotations;

namespace ServerEye.Core.Configuration;

public class GoApiSettings
{
    [Required(ErrorMessage = "GoApi BaseUrl is required")]
    public Uri BaseUrl { get; init; } = new Uri("http://127.0.0.1:8080");
    public Uri ProductionUrl { get; init; } = new Uri("https://api.servereye.dev");
    public string ApiKey { get; init; } = string.Empty;
    [Required(ErrorMessage = "GoApi ServiceId is required")]
    public string ServiceId { get; init; } = "csharp-backend";
    [Range(1, 300, ErrorMessage = "GoApi TimeoutSeconds must be between 1 and 300")]
    public int TimeoutSeconds { get; init; } = 30;
}
