namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiDataPoint
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("cpu_avg")]
    public double CpuAvg { get; init; }

    [JsonPropertyName("cpu_max")]
    public double CpuMax { get; init; }

    [JsonPropertyName("cpu_min")]
    public double CpuMin { get; init; }

    [JsonPropertyName("memory_avg")]
    public double MemoryAvg { get; init; }

    [JsonPropertyName("memory_max")]
    public double MemoryMax { get; init; }

    [JsonPropertyName("memory_min")]
    public double MemoryMin { get; init; }

    [JsonPropertyName("disk_avg")]
    public double DiskAvg { get; init; }

    [JsonPropertyName("disk_max")]
    public double DiskMax { get; init; }

    [JsonPropertyName("network_avg")]
    public double NetworkAvg { get; init; }

    [JsonPropertyName("network_max")]
    public double NetworkMax { get; init; }

    [JsonPropertyName("temp_avg")]
    public double TempAvg { get; init; }

    [JsonPropertyName("temp_max")]
    public double TempMax { get; init; }

    [JsonPropertyName("load_avg")]
    public double LoadAvg { get; init; }

    [JsonPropertyName("load_max")]
    public double LoadMax { get; init; }

    [JsonPropertyName("sample_count")]
    public int SampleCount { get; init; }

    // Additional memory details
    [JsonPropertyName("memory_cache_gb")]
    public double MemoryCacheGb { get; init; }

    [JsonPropertyName("memory_buffers_gb")]
    public double MemoryBuffersGb { get; init; }

    [JsonPropertyName("memory_available_gb")]
    public double MemoryAvailableGb { get; init; }

    [JsonPropertyName("memory_swap_percent")]
    public double MemorySwapPercent { get; init; }

    // Disk I/O metrics
    [JsonPropertyName("disk_read_mb")]
    public double DiskReadMb { get; init; }

    [JsonPropertyName("disk_write_mb")]
    public double DiskWriteMb { get; init; }

    [JsonPropertyName("disk_read_bytes_sec")]
    public double DiskReadBytesSec { get; init; }

    [JsonPropertyName("disk_write_bytes_sec")]
    public double DiskWriteBytesSec { get; init; }
}
