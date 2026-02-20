namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiSnapshotResponse
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("server_key")]
    public string ServerKey { get; set; } = string.Empty;

    [JsonPropertyName("metrics")]
    public GoApiSnapshotMetrics Metrics { get; set; } = new();

    [JsonPropertyName("status")]
    public GoApiServerStatus Status { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("alerts")]
    public List<string> Alerts { get; set; } = new();
}

public class GoApiSnapshotMetrics
{
    public double Cpu { get; set; }
    public double Memory { get; set; }
    public double Disk { get; set; }
    public double Network { get; set; }

    [JsonPropertyName("cpu_usage")]
    public GoApiCpuUsage CpuUsage { get; set; } = new();

    [JsonPropertyName("memory_details")]
    public GoApiMemoryDetails MemoryDetails { get; set; } = new();

    [JsonPropertyName("disk_details")]
    public List<GoApiDiskDetail> DiskDetails { get; set; } = new();

    [JsonPropertyName("network_details")]
    public GoApiNetworkDetails NetworkDetails { get; set; } = new();

    [JsonPropertyName("temperature_details")]
    public GoApiTemperatureDetails TemperatureDetails { get; set; } = new();

    [JsonPropertyName("system_details")]
    public GoApiSystemDetails SystemDetails { get; set; } = new();
}

public class GoApiCpuUsage
{
    [JsonPropertyName("usage_total")]
    public double UsageTotal { get; set; }

    [JsonPropertyName("usage_user")]
    public double UsageUser { get; set; }

    [JsonPropertyName("usage_system")]
    public double UsageSystem { get; set; }

    [JsonPropertyName("usage_idle")]
    public double UsageIdle { get; set; }

    [JsonPropertyName("load_average")]
    public GoApiLoadAverage LoadAverage { get; set; } = new();

    public int Cores { get; set; }
    public double Frequency { get; set; }
}

public class GoApiLoadAverage
{
    [JsonPropertyName("load_1min")]
    public double Load1Min { get; set; }

    [JsonPropertyName("load_5min")]
    public double Load5Min { get; set; }

    [JsonPropertyName("load_15min")]
    public double Load15Min { get; set; }
}

public class GoApiMemoryDetails
{
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; set; }

    [JsonPropertyName("used_gb")]
    public double UsedGb { get; set; }

    [JsonPropertyName("available_gb")]
    public double AvailableGb { get; set; }

    [JsonPropertyName("free_gb")]
    public double FreeGb { get; set; }

    [JsonPropertyName("buffers_gb")]
    public double BuffersGb { get; set; }

    [JsonPropertyName("cached_gb")]
    public double CachedGb { get; set; }

    [JsonPropertyName("used_percent")]
    public double UsedPercent { get; set; }
}

public class GoApiDiskDetail
{
    public string Path { get; set; } = string.Empty;
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; set; }
    [JsonPropertyName("used_gb")]
    public double UsedGb { get; set; }
    [JsonPropertyName("free_gb")]
    public double FreeGb { get; set; }
    [JsonPropertyName("used_percent")]
    public double UsedPercent { get; set; }
    public string Filesystem { get; set; } = string.Empty;
}

public class GoApiNetworkDetails
{
    [JsonPropertyName("interfaces")]
    public List<GoApiNetworkInterface> Interfaces { get; set; } = new();

    [JsonPropertyName("total_rx_mbps")]
    public double TotalRxMbps { get; set; }

    [JsonPropertyName("total_tx_mbps")]
    public double TotalTxMbps { get; set; }
}

public class GoApiNetworkInterface
{
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("rx_bytes")]
    public long RxBytes { get; set; }
    [JsonPropertyName("tx_bytes")]
    public long TxBytes { get; set; }
    [JsonPropertyName("rx_packets")]
    public long RxPackets { get; set; }
    [JsonPropertyName("tx_packets")]
    public long TxPackets { get; set; }
    [JsonPropertyName("rx_speed_mbps")]
    public double RxSpeedMbps { get; set; }
    [JsonPropertyName("tx_speed_mbps")]
    public double TxSpeedMbps { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class GoApiTemperatureDetails
{
    [JsonPropertyName("cpu_temperature")]
    public double CpuTemperature { get; set; }

    [JsonPropertyName("gpu_temperature")]
    public double GpuTemperature { get; set; }

    [JsonPropertyName("system_temperature")]
    public double SystemTemperature { get; set; }

    [JsonPropertyName("storage_temperatures")]
    public List<GoApiStorageTemperature> StorageTemperatures { get; set; } = new();

    [JsonPropertyName("highest_temperature")]
    public double HighestTemperature { get; set; }

    [JsonPropertyName("temperature_unit")]
    public string TemperatureUnit { get; set; } = string.Empty;
}

public class GoApiStorageTemperature
{
    public string Device { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Temperature { get; set; }
}

public class GoApiSystemDetails
{
    public string Hostname { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string Kernel { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    [JsonPropertyName("uptime_seconds")]
    public long UptimeSeconds { get; set; }
    [JsonPropertyName("uptime_human")]
    public string UptimeHuman { get; set; } = string.Empty;
    [JsonPropertyName("processes_total")]
    public int ProcessesTotal { get; set; }
    [JsonPropertyName("processes_running")]
    public int ProcessesRunning { get; set; }
    [JsonPropertyName("processes_sleeping")]
    public int ProcessesSleeping { get; set; }
}
