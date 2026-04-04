namespace ServerEye.Core.Configuration;

using System.Diagnostics.CodeAnalysis;

public class FrontendSettings
{
    [SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "String type required for configuration binding")]
    public string BaseUrl { get; init; } = "http://127.0.0.1:3000";
}
