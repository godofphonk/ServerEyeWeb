namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiMetricsResponse
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; init; } = string.Empty;

    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; init; }

    [JsonPropertyName("end_time")]
    public DateTime EndTime { get; init; }

    [JsonPropertyName("granularity")]
    public string Granularity { get; init; } = string.Empty;

    [JsonPropertyName("data_points")]
    public List<GoApiDataPoint> DataPoints { get; init; } = new();

    [JsonPropertyName("total_points")]
    public int TotalPoints { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("status")]
    public GoApiServerStatus? Status { get; init; }

    [JsonPropertyName("temperature_details")]
    public TemperatureDetails? TemperatureDetails { get; init; }

    [JsonPropertyName("network_details")]
    public NetworkDetails? NetworkDetails { get; init; }

    [JsonPropertyName("disk_details")]
    public DiskDetails? DiskDetails { get; init; }
}
