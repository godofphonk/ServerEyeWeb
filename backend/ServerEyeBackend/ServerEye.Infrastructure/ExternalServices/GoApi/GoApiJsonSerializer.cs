namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;

/// <summary>
/// JSON serialization/deserialization for Go API responses.
/// </summary>
public static class GoApiJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    private static readonly JsonSerializerOptions DebugOptions = new() { WriteIndented = true };

    private static ILogger? _logger;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Deserializes JSON content to specified type.
    /// </summary>
    public static T Deserialize<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, DefaultOptions)!;
    }

    /// <summary>
    /// Attempts to deserialize without throwing exception.
    /// </summary>
    public static T? TryDeserialize<T>(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(content, DefaultOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Deserializes Go API metrics response.
    /// </summary>
    public static GoApiMetricsResponse? DeserializeMetricsResponse(string content)
    {
        return TryDeserialize<GoApiMetricsResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API snapshot response.
    /// </summary>
    public static GoApiSnapshotResponse? DeserializeSnapshotResponse(string content)
    {
        return TryDeserialize<GoApiSnapshotResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API unified response.
    /// </summary>
    public static GoApiUnifiedResponse? DeserializeUnifiedResponse(string content)
    {
        try
        {
            // First, try to deserialize as standard format
            var response = TryDeserialize<GoApiUnifiedResponse>(content);

            if (response?.Metrics != null && response.Metrics.DataPoints != null && response.Metrics.DataPoints.Count > 0)
            {
                // Standard format with dataPoints - return as-is
                return response;
            }

            // If no dataPoints, try to parse as snapshot format and transform
            var snapshotResponse = TryDeserialize<GoApiUnifiedSnapshotFormat>(content);
            if (snapshotResponse?.Metrics?.Snapshot != null)
            {
                _logger?.LogInformation(
                    "Go API unified snapshot response: ServerKey={ServerKey}, CpuPercent={CpuPercent}, MemoryPercent={MemoryPercent}, TemperatureCelsius={TemperatureCelsius}, LoadAverage={LoadAverage}",
                    snapshotResponse.ServerKey,
                    snapshotResponse.Metrics.Snapshot.CpuPercent,
                    snapshotResponse.Metrics.Snapshot.MemoryPercent,
                    snapshotResponse.Metrics.Snapshot.TemperatureCelsius,
                    snapshotResponse.Metrics.Snapshot.LoadAverage != null ? snapshotResponse.Metrics.Snapshot.LoadAverage.Load1Min : 0);

                _logger?.LogInformation(
                    "Go API unified static_info: MemoryInfo={MemoryInfo}, Type={Type}, SpeedMhz={SpeedMhz}",
                    snapshotResponse.StaticInfo?.MemoryInfo != null ? "present" : "null",
                    snapshotResponse.StaticInfo?.MemoryInfo?.Type ?? "null",
                    snapshotResponse.StaticInfo?.MemoryInfo?.SpeedMhz ?? 0);

                _logger?.LogInformation(
                    "Go API unified temperatures: Cpu={Cpu}, Gpu={Gpu}, Highest={Highest}",
                    snapshotResponse.Metrics.Snapshot.TemperatureDetails?.CpuTemperature ?? 0,
                    snapshotResponse.Metrics.Snapshot.TemperatureDetails?.GpuTemperature ?? 0,
                    snapshotResponse.Metrics.Snapshot.TemperatureDetails?.HighestTemperature ?? 0);

                return TransformSnapshotToUnified(snapshotResponse);
            }

            return response;
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "JSON deserialization failed for unified response. Content preview: {ContentPreview}", content[..Math.Min(500, content.Length)]);
            return null;
        }
    }

    /// <summary>
    /// Deserializes Go API server info.
    /// </summary>
    public static GoApiServerInfo? DeserializeServerInfo(string content)
    {
        return TryDeserialize<GoApiServerInfo>(content);
    }

    /// <summary>
    /// Deserializes Go API server status.
    /// </summary>
    public static GoApiServerStatus? DeserializeServerStatus(string content)
    {
        return TryDeserialize<GoApiServerStatus>(content);
    }

    /// <summary>
    /// Deserializes Go API static info response.
    /// </summary>
    public static GoApiStaticInfoResponse? DeserializeStaticInfoResponse(string content)
    {
        return TryDeserialize<GoApiStaticInfoResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API servers list.
    /// </summary>
    public static List<GoApiServerInfo>? DeserializeServersList(string content)
    {
        return TryDeserialize<List<GoApiServerInfo>>(content);
    }

    /// <summary>
    /// Deserializes Go API source response.
    /// </summary>
    public static GoApiSourceResponse? DeserializeSourceResponse(string content)
    {
        return TryDeserialize<GoApiSourceResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API source identifiers response.
    /// </summary>
    public static GoApiSourceIdentifiersResponse? DeserializeSourceIdentifiersResponse(string content)
    {
        return TryDeserialize<GoApiSourceIdentifiersResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API delete source response.
    /// </summary>
    public static GoApiDeleteSourceResponse? DeserializeDeleteSourceResponse(string content)
    {
        return TryDeserialize<GoApiDeleteSourceResponse>(content);
    }

    /// <summary>
    /// Serializes object to JSON for debugging.
    /// </summary>
    public static string SerializeForDebug<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, DebugOptions);
    }

    /// <summary>
    /// Transforms snapshot format to unified response with RawMetricsResponse.
    /// </summary>
    private static GoApiUnifiedResponse TransformSnapshotToUnified(GoApiUnifiedSnapshotFormat snapshot)
    {
        if (snapshot.Metrics?.Snapshot == null)
        {
            return new GoApiUnifiedResponse
            {
                ServerKey = snapshot.ServerKey,
                Status = snapshot.Status,
                StaticInfo = snapshot.StaticInfo,
                UptimeSeconds = snapshot.UptimeSeconds,
                Timestamp = snapshot.Timestamp
            };
        }

        var snap = snapshot.Metrics.Snapshot;
        var tempDetails = snap.TemperatureDetails;
        var loadAvg = snap.LoadAverage;

        // Create a single data point from snapshot
        var dataPoint = new GoApiDataPoint
        {
            Timestamp = DateTime.UtcNow,
            CpuAvg = snap.CpuPercent,
            CpuMax = snap.CpuPercent,
            CpuMin = snap.CpuPercent,
            MemoryAvg = snap.MemoryPercent,
            MemoryMax = snap.MemoryPercent,
            MemoryMin = snap.MemoryPercent,
            DiskAvg = snap.DiskPercent,
            DiskMax = snap.DiskPercent,
            NetworkAvg = snap.NetworkMbps,
            NetworkMax = snap.NetworkMbps,
            TempAvg = snap.TemperatureCelsius,
            TempMax = snap.TemperatureCelsius,
            LoadAvg = loadAvg?.Load1Min ?? 0,
            LoadMax = loadAvg?.Load1Min ?? 0,
            SampleCount = 1,

            // Nested objects for frontend compatibility
            Network = new NetworkMetrics { Avg = snap.NetworkMbps, Max = snap.NetworkMbps },
            LoadAverage = new LoadAverageMetrics { Avg = loadAvg?.Load1Min ?? 0, Max = loadAvg?.Load1Min ?? 0 },
            Temperature = new TemperatureMetrics { Avg = snap.TemperatureCelsius, Max = snap.TemperatureCelsius },
            TemperatureDetails = tempDetails != null
                ? new TemperatureDetails
                {
                    CpuTemperature = tempDetails.CpuTemperature,
                    GpuTemperature = tempDetails.GpuTemperature,
                    SystemTemperature = tempDetails.SystemTemperature,
                    HighestTemperature = tempDetails.HighestTemperature,
                    TemperatureUnit = tempDetails.TemperatureUnit,
                    Storage = tempDetails.Storage
                }
                : null,

            // Memory details
            MemoryCacheGb = snap.MemoryDetails?.CachedGb ?? 0,
            MemoryBuffersGb = snap.MemoryDetails?.BuffersGb ?? 0,
            MemoryAvailableGb = snap.MemoryDetails?.AvailableGb ?? 0,
        };

        // Create summary from snapshot
        var summary = new MetricsSummary
        {
            AvgCpu = snap.CpuPercent,
            MaxCpu = snap.CpuPercent,
            MinCpu = snap.CpuPercent,
            AvgMemory = snap.MemoryPercent,
            MaxMemory = snap.MemoryPercent,
            MinMemory = snap.MemoryPercent,
            AvgDisk = snap.DiskPercent,
            MaxDisk = snap.DiskPercent,
            TotalDataPoints = 1,
            TimeRange = TimeSpan.Zero
        };

        return new GoApiUnifiedResponse
        {
            ServerKey = snapshot.ServerKey,
            Metrics = new RawMetricsResponse
            {
                ServerId = snapshot.Metrics.ServerId,
                ServerName = string.Empty,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Granularity = "minute",
                DataPoints = new List<GoApiDataPoint> { dataPoint },
                TotalPoints = 1,
                Summary = summary,
                TemperatureDetails = snap.Temperatures != null
                    ? new TemperatureDetails
                    {
                        CpuTemperature = snap.Temperatures.CpuTemperature,
                        GpuTemperature = snap.Temperatures.GpuTemperature,
                        SystemTemperature = snap.Temperatures.SystemTemperature,
                        HighestTemperature = snap.Temperatures.HighestTemperature,
                        TemperatureUnit = snap.Temperatures.TemperatureUnit,
                        Storage = snap.Temperatures.Storage
                    }
                    : new TemperatureDetails
                    {
                        CpuTemperature = snap.TemperatureCelsius,
                        GpuTemperature = 0,
                        SystemTemperature = 0,
                        HighestTemperature = snap.TemperatureCelsius,
                        TemperatureUnit = "celsius"
                    },
                NetworkDetails = snap.NetworkDetails != null
                    ? new NetworkDetails
                    {
                        Interfaces = snap.NetworkDetails.Interfaces.Select(i => new NetworkInterface
                        {
                            Name = i.Name,
                            RxBytes = i.RxBytes,
                            TxBytes = i.TxBytes,
                            RxPackets = i.RxPackets,
                            TxPackets = i.TxPackets,
                            Status = i.Status
                        }).ToList(),
                        TotalRx = (long)(snap.NetworkDetails.TotalRxMbps * 1000000),
                        TotalTx = (long)(snap.NetworkDetails.TotalTxMbps * 1000000),
                        Timestamp = DateTime.UtcNow
                    }
                    : null,
                DiskDetails = snap.DiskDetails != null
                    ? new DiskDetails
                    {
                        Disks = snap.DiskDetails.Select(d => new DiskInfo
                        {
                            Path = d.Path,
                            TotalGb = d.TotalGb,
                            UsedGb = d.UsedGb,
                            FreeGb = d.FreeGb,
                            UsedPercent = d.UsedPercent,
                            Filesystem = d.Filesystem
                        }).ToList()
                    }
                    : null,
                IsCached = false
            },
            Status = snapshot.Status,
            StaticInfo = snapshot.StaticInfo,
            UptimeSeconds = snap.UptimeSeconds,
            Timestamp = snapshot.Timestamp
        };
    }
}

/// <summary>
/// Snapshot format returned by Go API unified endpoint.
/// </summary>
public class GoApiUnifiedSnapshotFormat
{
    [JsonPropertyName("server_key")]
    public string ServerKey { get; init; } = string.Empty;

    [JsonPropertyName("metrics")]
    public GoApiUnifiedMetricsWrapper? Metrics { get; init; }

    [JsonPropertyName("status")]
    public GoApiServerStatus? Status { get; init; }

    [JsonPropertyName("static_info")]
    public GoApiStaticInfo? StaticInfo { get; init; }

    [JsonPropertyName("uptime_seconds")]
    public long? UptimeSeconds { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public class GoApiUnifiedMetricsWrapper
{
    [JsonPropertyName("metrics")]
    public GoApiUnifiedSnapshotMetrics? Snapshot { get; init; }

    [JsonPropertyName("server_id")]
    public string ServerId { get; init; } = string.Empty;
}

/// <summary>
/// Snapshot metrics format returned by Go API unified endpoint (flat format).
/// </summary>
public class GoApiUnifiedSnapshotMetrics
{
    [JsonPropertyName("cpu_percent")]
    public double CpuPercent { get; init; }

    [JsonPropertyName("memory_percent")]
    public double MemoryPercent { get; init; }

    [JsonPropertyName("disk_percent")]
    public double DiskPercent { get; init; }

    [JsonPropertyName("network_mbps")]
    public double NetworkMbps { get; init; }

    [JsonPropertyName("temperature_celsius")]
    public double TemperatureCelsius { get; init; }

    [JsonPropertyName("load_average")]
    public GoApiLoadAverage? LoadAverage { get; init; }

    [JsonPropertyName("temperatures")]
    public GoApiTemperatureDetails? Temperatures { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("memory_details")]
    public GoApiMemoryDetails? MemoryDetails { get; init; }

    [JsonPropertyName("disk_details")]
    public List<GoApiDiskDetail>? DiskDetails { get; init; }

    [JsonPropertyName("network_details")]
    public GoApiNetworkDetails? NetworkDetails { get; init; }

    [JsonPropertyName("temperature_details")]
    public GoApiTemperatureDetails? TemperatureDetails { get; init; }

    [JsonPropertyName("uptime_seconds")]
    public long? UptimeSeconds { get; init; }
}
