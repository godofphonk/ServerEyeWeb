namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiDataPoint
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("cpu_avg")]
    public double CpuAvg { get; set; }

    [JsonPropertyName("cpu_max")]
    public double CpuMax { get; set; }

    [JsonPropertyName("cpu_min")]
    public double CpuMin { get; set; }

    [JsonPropertyName("memory_avg")]
    public double MemoryAvg { get; set; }

    [JsonPropertyName("memory_max")]
    public double MemoryMax { get; set; }

    [JsonPropertyName("memory_min")]
    public double MemoryMin { get; set; }

    [JsonPropertyName("disk_avg")]
    public double DiskAvg { get; set; }

    [JsonPropertyName("disk_max")]
    public double DiskMax { get; set; }

    [JsonPropertyName("network_avg")]
    public double NetworkAvg { get; set; }

    [JsonPropertyName("network_max")]
    public double NetworkMax { get; set; }

    [JsonPropertyName("temp_avg")]
    public double TempAvg { get; set; }

    [JsonPropertyName("temp_max")]
    public double TempMax { get; set; }

    [JsonPropertyName("load_avg")]
    public double LoadAvg { get; set; }

    [JsonPropertyName("load_max")]
    public double LoadMax { get; set; }

    [JsonPropertyName("sample_count")]
    public int SampleCount { get; set; }
}
