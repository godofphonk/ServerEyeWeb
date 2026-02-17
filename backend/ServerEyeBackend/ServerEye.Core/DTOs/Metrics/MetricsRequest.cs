namespace ServerEye.Core.DTOs.Metrics;

using System.Text.Json.Serialization;

public class MetricsRequest
{
    [JsonPropertyName("start")]
    public DateTime Start { get; set; }

    [JsonPropertyName("end")]
    public DateTime End { get; set; }

    [JsonPropertyName("granularity")]
    public string? Granularity { get; set; }
}
