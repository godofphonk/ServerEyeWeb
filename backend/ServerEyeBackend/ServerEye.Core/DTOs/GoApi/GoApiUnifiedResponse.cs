namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;
using ServerEye.Core.DTOs.Metrics;

/// <summary>
/// Unified response containing metrics, status, and static info for a server.
/// </summary>
public class GoApiUnifiedResponse
{
    [JsonPropertyName("server_key")]
    public string ServerKey { get; init; } = string.Empty;

    [JsonPropertyName("metrics")]
    public RawMetricsResponse? Metrics { get; init; }

    [JsonPropertyName("status")]
    public GoApiServerStatus? Status { get; init; }

    [JsonPropertyName("static_info")]
    public GoApiStaticInfo? StaticInfo { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
