namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiSnapshotResponse
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; init; } = string.Empty;

    [JsonPropertyName("server_key")]
    public string ServerKey { get; init; } = string.Empty;

    [JsonPropertyName("metrics")]
    public GoApiSnapshotMetrics Metrics { get; init; } = new();

    [JsonPropertyName("status")]
    public GoApiServerStatus Status { get; init; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("alerts")]
    public List<string> Alerts { get; init; } = new();
}

public class GoApiSnapshotMetrics
{
    public double Cpu { get; init; }
    public double Memory { get; init; }
    public double Disk { get; init; }
    public double Network { get; init; }

    [JsonPropertyName("cpu_usage")]
    public GoApiCpuUsage CpuUsage { get; init; } = new();

    [JsonPropertyName("memory_details")]
    public GoApiMemoryDetails MemoryDetails { get; init; } = new();

    [JsonPropertyName("disk_details")]
    public List<GoApiDiskDetail> DiskDetails { get; init; } = new();

    [JsonPropertyName("network_details")]
    public GoApiNetworkDetails NetworkDetails { get; init; } = new();

    [JsonPropertyName("temperature_details")]
    public GoApiTemperatureDetails TemperatureDetails { get; init; } = new();

    [JsonPropertyName("system_details")]
    public GoApiSystemDetails SystemDetails { get; init; } = new();
}

public class GoApiCpuUsage
{
    [JsonPropertyName("usage_total")]
    public double UsageTotal { get; init; }

    [JsonPropertyName("usage_user")]
    public double UsageUser { get; init; }

    [JsonPropertyName("usage_system")]
    public double UsageSystem { get; init; }

    [JsonPropertyName("usage_idle")]
    public double UsageIdle { get; init; }

    [JsonPropertyName("load_average")]
    public GoApiLoadAverage LoadAverage { get; init; } = new();

    public int Cores { get; init; }
    public double Frequency { get; init; }
}

public class GoApiLoadAverage
{
    [JsonPropertyName("load_1min")]
    public double Load1Min { get; init; }

    [JsonPropertyName("load_5min")]
    public double Load5Min { get; init; }

    [JsonPropertyName("load_15min")]
    public double Load15Min { get; init; }
}

public class GoApiMemoryDetails
{
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; init; }

    [JsonPropertyName("used_gb")]
    public double UsedGb { get; init; }

    [JsonPropertyName("available_gb")]
    public double AvailableGb { get; init; }

    [JsonPropertyName("free_gb")]
    public double FreeGb { get; init; }

    [JsonPropertyName("buffers_gb")]
    public double BuffersGb { get; init; }

    [JsonPropertyName("cached_gb")]
    public double CachedGb { get; init; }

    [JsonPropertyName("used_percent")]
    public double UsedPercent { get; init; }
}

public class GoApiDiskDetail
{
    public string Path { get; init; } = string.Empty;
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; init; }
    [JsonPropertyName("used_gb")]
    public double UsedGb { get; init; }
    [JsonPropertyName("free_gb")]
    public double FreeGb { get; init; }
    [JsonPropertyName("used_percent")]
    public double UsedPercent { get; init; }
    public string Filesystem { get; init; } = string.Empty;
}

public class GoApiNetworkDetails
{
    [JsonPropertyName("interfaces")]
    public List<GoApiNetworkInterface> Interfaces { get; init; } = new();

    [JsonPropertyName("total_rx_mbps")]
    public double TotalRxMbps { get; init; }

    [JsonPropertyName("total_tx_mbps")]
    public double TotalTxMbps { get; init; }
}

public class GoApiNetworkInterface
{
    public string Name { get; init; } = string.Empty;
    [JsonPropertyName("rx_bytes")]
    public long RxBytes { get; init; }
    [JsonPropertyName("tx_bytes")]
    public long TxBytes { get; init; }
    [JsonPropertyName("rx_packets")]
    public long RxPackets { get; init; }
    [JsonPropertyName("tx_packets")]
    public long TxPackets { get; init; }
    [JsonPropertyName("rx_speed_mbps")]
    public double RxSpeedMbps { get; init; }
    [JsonPropertyName("tx_speed_mbps")]
    public double TxSpeedMbps { get; init; }
    public string Status { get; init; } = string.Empty;
}

public class GoApiTemperatureDetails
{
    [JsonPropertyName("cpu_temperature")]
    public double CpuTemperature { get; init; }

    [JsonPropertyName("gpu_temperature")]
    public double GpuTemperature { get; init; }

    [JsonPropertyName("system_temperature")]
    public double SystemTemperature { get; init; }

    [JsonPropertyName("storage_temperatures")]
    public List<GoApiStorageTemperature> StorageTemperatures { get; init; } = new();

    [JsonPropertyName("highest_temperature")]
    public double HighestTemperature { get; init; }

    [JsonPropertyName("temperature_unit")]
    public string TemperatureUnit { get; init; } = string.Empty;
}

public class GoApiStorageTemperature
{
    public string Device { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public double Temperature { get; init; }
}

public class GoApiSystemDetails
{
    public string Hostname { get; init; } = string.Empty;
    public string Os { get; init; } = string.Empty;
    public string Kernel { get; init; } = string.Empty;
    public string Architecture { get; init; } = string.Empty;
    [JsonPropertyName("uptime_seconds")]
    public long UptimeSeconds { get; init; }
    [JsonPropertyName("uptime_human")]
    public string UptimeHuman { get; init; } = string.Empty;
    [JsonPropertyName("processes_total")]
    public int ProcessesTotal { get; init; }
    [JsonPropertyName("processes_running")]
    public int ProcessesRunning { get; init; }
    [JsonPropertyName("processes_sleeping")]
    public int ProcessesSleeping { get; init; }
}
