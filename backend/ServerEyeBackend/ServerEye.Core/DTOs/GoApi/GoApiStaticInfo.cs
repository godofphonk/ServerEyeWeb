namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiStaticInfo
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("operating_system")]
    public string OperatingSystem { get; set; } = string.Empty;

    [JsonPropertyName("agent_version")]
    public string AgentVersion { get; set; } = string.Empty;

    [JsonPropertyName("cpu_info")]
    public StaticCpuInfo? CpuInfo { get; set; }

    [JsonPropertyName("memory_info")]
    public StaticMemoryInfo? MemoryInfo { get; set; }

    [JsonPropertyName("disk_info")]
    public List<StaticDiskInfo> DiskInfo { get; set; } = new();

    [JsonPropertyName("network_interfaces")]
    public List<StaticNetworkInterface> NetworkInterfaces { get; set; } = new();

    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; set; }
}

public class StaticCpuInfo
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("cores")]
    public int Cores { get; set; }

    [JsonPropertyName("threads")]
    public int Threads { get; set; }

    [JsonPropertyName("frequency_mhz")]
    public double FrequencyMhz { get; set; }
}

public class StaticMemoryInfo
{
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("speed_mhz")]
    public double SpeedMhz { get; set; }
}

public class StaticDiskInfo
{
    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;

    [JsonPropertyName("size_gb")]
    public double SizeGb { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}

public class StaticNetworkInterface
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("speed_mbps")]
    public long SpeedMbps { get; set; }

    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; } = string.Empty;
}
