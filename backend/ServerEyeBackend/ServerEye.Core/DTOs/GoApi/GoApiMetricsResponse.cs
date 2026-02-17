namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiMetricsResponse
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("granularity")]
    public string Granularity { get; set; } = string.Empty;

    [JsonPropertyName("data_points")]
    public List<GoApiDataPoint> DataPoints { get; set; } = new();

    [JsonPropertyName("total_points")]
    public int TotalPoints { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public GoApiServerStatus? Status { get; set; }

    [JsonPropertyName("temperature_details")]
    public TemperatureDetails? TemperatureDetails { get; set; }

    [JsonPropertyName("network_details")]
    public NetworkDetails? NetworkDetails { get; set; }
}
