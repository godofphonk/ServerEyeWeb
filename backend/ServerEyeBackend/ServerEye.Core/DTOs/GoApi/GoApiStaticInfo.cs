namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiStaticInfo
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; init; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; init; } = string.Empty;

    [JsonPropertyName("operating_system")]
    public string OperatingSystem { get; init; } = string.Empty;

    [JsonPropertyName("kernel")]
    public string Kernel { get; init; } = string.Empty;

    [JsonPropertyName("architecture")]
    public string Architecture { get; init; } = string.Empty;

    [JsonPropertyName("agent_version")]
    public string AgentVersion { get; init; } = string.Empty;

    [JsonPropertyName("cpu_info")]
    public StaticCpuInfo? CpuInfo { get; init; }

    [JsonPropertyName("memory_info")]
    public StaticMemoryInfo? MemoryInfo { get; init; }

    [JsonPropertyName("motherboard_info")]
    public StaticMotherboardInfo? MotherboardInfo { get; init; }

    [JsonPropertyName("gpu_info")]
    public StaticGpuInfo? GpuInfo { get; init; }

    [JsonPropertyName("disk_info")]
    public List<StaticDiskInfo> DiskInfo { get; init; } = new();

    [JsonPropertyName("network_interfaces")]
    public List<StaticNetworkInterface> NetworkInterfaces { get; init; } = new();

    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; init; }
}

public class StaticCpuInfo
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("cores")]
    public int Cores { get; init; }

    [JsonPropertyName("threads")]
    public int Threads { get; init; }

    [JsonPropertyName("frequency_mhz")]
    public double FrequencyMhz { get; init; }
}

public class StaticMemoryInfo
{
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("speed_mhz")]
    public double SpeedMhz { get; init; }
}

public class StaticMotherboardInfo
{
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; init; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("bios_date")]
    public DateTime? BiosDate { get; init; }
}

public class StaticGpuInfo
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("driver")]
    public string Driver { get; init; } = string.Empty;

    [JsonPropertyName("memory_gb")]
    public double MemoryGb { get; init; }
}

public class StaticDiskInfo
{
    [JsonPropertyName("device")]
    public string Device { get; init; } = string.Empty;

    [JsonPropertyName("size_gb")]
    public double SizeGb { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;
}

public class StaticNetworkInterface
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("speed_mbps")]
    public long SpeedMbps { get; init; }

    [JsonPropertyName("mac_address")]
    public string MacAddress { get; init; } = string.Empty;
}
