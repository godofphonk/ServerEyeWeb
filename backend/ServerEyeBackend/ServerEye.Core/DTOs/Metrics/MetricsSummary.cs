namespace ServerEye.Core.DTOs.Metrics;

using System.Text.Json.Serialization;

public class MetricsSummary
{
    [JsonPropertyName("avgCpu")]
    public double AvgCpu { get; set; }

    [JsonPropertyName("maxCpu")]
    public double MaxCpu { get; set; }

    [JsonPropertyName("minCpu")]
    public double MinCpu { get; set; }

    [JsonPropertyName("avgMemory")]
    public double AvgMemory { get; set; }

    [JsonPropertyName("maxMemory")]
    public double MaxMemory { get; set; }

    [JsonPropertyName("minMemory")]
    public double MinMemory { get; set; }

    [JsonPropertyName("avgDisk")]
    public double AvgDisk { get; set; }

    [JsonPropertyName("maxDisk")]
    public double MaxDisk { get; set; }

    [JsonPropertyName("totalDataPoints")]
    public int TotalDataPoints { get; set; }

    [JsonPropertyName("timeRange")]
    public TimeSpan TimeRange { get; set; }
}
