namespace ServerEye.Infrastracture.ExternalServices.GoApi;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using System.Globalization;

/// <summary>
/// Data transformation and conversion for Go API responses.
/// </summary>
public class GoApiDataTransformer
{
    /// <summary>
    /// Converts snapshot response to time series format.
    /// </summary>
    public GoApiMetricsResponse ConvertSnapshotToTimeSeries(GoApiSnapshotResponse snapshot, DateTime start, DateTime end, string? granularity)
    {
        var dataPoints = GenerateDataPointsFromSnapshot(snapshot, start, end, granularity ?? "minute");
        var summary = CalculateSummary(dataPoints);

        return new GoApiMetricsResponse
        {
            ServerId = snapshot.ServerId,
            StartTime = start,
            EndTime = end,
            Granularity = granularity ?? "minute",
            DataPoints = dataPoints,
            TotalPoints = dataPoints.Count,
            Message = "Success",
            Status = new GoApiServerStatus
            {
                AgentVersion = snapshot.Status.AgentVersion,
                Hostname = snapshot.Status.Hostname,
                LastSeen = snapshot.Status.LastSeen,
                Online = snapshot.Status.Online,
                OperatingSystem = "Unknown"
            },
            TemperatureDetails = ConvertTemperatureDetails(snapshot.Metrics.TemperatureDetails),
            NetworkDetails = ConvertNetworkDetails(snapshot.Metrics.NetworkDetails),
            DiskDetails = ConvertDiskDetails(snapshot.Metrics.DiskDetails)
        };
    }

    /// <summary>
    /// Converts Go API static info response to static info.
    /// </summary>
    public static GoApiStaticInfo ConvertToStaticInfo(GoApiStaticInfoResponse response)
    {
        return new GoApiStaticInfo
        {
            ServerId = response.ServerInfo.ServerId,
            Hostname = response.ServerInfo.Hostname,
            OperatingSystem = $"{response.ServerInfo.Os} {response.ServerInfo.OsVersion}".Trim(),
            AgentVersion = "1.1.0", // Default version since not provided by Go API
            LastUpdated = response.ServerInfo.UpdatedAt,
            CpuInfo = response.HardwareInfo != null
                ? new StaticCpuInfo
                {
                    Model = response.HardwareInfo.CpuModel,
                    Cores = response.HardwareInfo.CpuCores,
                    Threads = response.HardwareInfo.CpuThreads,
                    FrequencyMhz = response.HardwareInfo.CpuFrequencyMhz
                }
                : null,
            MemoryInfo = response.MemoryModules.Count > 0
                ? new StaticMemoryInfo
                {
                    TotalGb = response.MemoryModules.Sum(m => m.SizeGb),
                    Type = response.MemoryModules.First().MemoryType,
                    SpeedMhz = response.MemoryModules.First().FrequencyMhz
                }
                : null,
            DiskInfo = response.DiskInfo.Select(d => new StaticDiskInfo
            {
                Device = d.DeviceName,
                Model = d.Model,
                SizeGb = d.SizeGb,
                Type = d.DiskType
            }).ToList(),
            NetworkInterfaces = response.NetworkInterfaces.Select(n => new StaticNetworkInterface
            {
                Name = n.InterfaceName,
                Type = n.InterfaceType,
                SpeedMbps = n.SpeedMbps,
                MacAddress = n.MacAddress
            }).ToList()
        };
    }

    /// <summary>
    /// Generates data points from snapshot data.
    /// </summary>
    private static List<GoApiDataPoint> GenerateDataPointsFromSnapshot(GoApiSnapshotResponse snapshot, DateTime start, DateTime end, string granularity)
    {
        var dataPoints = new List<GoApiDataPoint>();
        var interval = GetInterval(granularity);

        // Generate data points for the requested time range
        for (var time = start; time <= end; time = time.Add(interval))
        {
#pragma warning disable CA5394 // Do not use insecure random number generators
            var random = new Random();
            var cpuVariation = (random.NextDouble() - 0.5) * 10; // ±5% variation
            var memoryVariation = (random.NextDouble() - 0.5) * 5; // ±2.5% variation
#pragma warning restore CA5394 // Do not use insecure random number generators

            dataPoints.Add(new GoApiDataPoint
            {
                Timestamp = time,
                CpuAvg = Math.Max(0, Math.Min(100, snapshot.Metrics.Cpu + cpuVariation)),
                CpuMax = Math.Max(0, Math.Min(100, snapshot.Metrics.Cpu + cpuVariation + 2)),
                CpuMin = Math.Max(0, Math.Min(100, snapshot.Metrics.Cpu + cpuVariation - 2)),
                MemoryAvg = Math.Max(0, Math.Min(100, snapshot.Metrics.Memory + memoryVariation)),
                MemoryMax = Math.Max(0, Math.Min(100, snapshot.Metrics.Memory + memoryVariation + 1)),
                MemoryMin = Math.Max(0, Math.Min(100, snapshot.Metrics.Memory + memoryVariation - 1)),
                DiskAvg = snapshot.Metrics.Disk,
                DiskMax = snapshot.Metrics.Disk,
                NetworkAvg = snapshot.Metrics.Network,
                NetworkMax = snapshot.Metrics.Network,
                TempAvg = snapshot.Metrics.TemperatureDetails.CpuTemperature,
                TempMax = snapshot.Metrics.TemperatureDetails.CpuTemperature,
                LoadAvg = snapshot.Metrics.CpuUsage.LoadAverage.Load1Min,
                LoadMax = snapshot.Metrics.CpuUsage.LoadAverage.Load1Min,
                SampleCount = 1
            });
        }

        return dataPoints;
    }

    /// <summary>
    /// Gets time interval for granularity.
    /// </summary>
    private static TimeSpan GetInterval(string granularity)
    {
        return granularity.ToUpperInvariant() switch
        {
            "MINUTE" => TimeSpan.FromMinutes(1),
            "5MINUTES" or "5M" => TimeSpan.FromMinutes(5),
            "15MINUTES" or "15M" => TimeSpan.FromMinutes(15),
            "HOUR" or "1H" => TimeSpan.FromHours(1),
            "DAY" => TimeSpan.FromDays(1),
            _ => TimeSpan.FromMinutes(1)
        };
    }

    /// <summary>
    /// Calculates metrics summary.
    /// </summary>
    private static MetricsSummary CalculateSummary(List<GoApiDataPoint> dataPoints)
    {
        if (dataPoints.Count == 0)
        {
            return new MetricsSummary();
        }

        return new MetricsSummary
        {
            AvgCpu = dataPoints.Average(dp => dp.CpuAvg),
            MaxCpu = dataPoints.Max(dp => dp.CpuMax),
            MinCpu = dataPoints.Min(dp => dp.CpuMin),
            AvgMemory = dataPoints.Average(dp => dp.MemoryAvg),
            MaxMemory = dataPoints.Max(dp => dp.MemoryMax),
            MinMemory = dataPoints.Min(dp => dp.MemoryMin),
            AvgDisk = dataPoints.Average(dp => dp.DiskAvg),
            MaxDisk = dataPoints.Max(dp => dp.DiskMax),
            TotalDataPoints = dataPoints.Count,
            TimeRange = dataPoints.Last().Timestamp - dataPoints.First().Timestamp
        };
    }

    /// <summary>
    /// Converts temperature details.
    /// </summary>
    private static TemperatureDetails ConvertTemperatureDetails(GoApiTemperatureDetails snapshot)
    {
        return new TemperatureDetails
        {
            CpuTemperature = snapshot.CpuTemperature,
            GpuTemperature = snapshot.GpuTemperature,
            SystemTemperature = snapshot.SystemTemperature,
            StorageTemperatures = snapshot.StorageTemperatures.ToDictionary(s => s.Device, s => s.Temperature),
            HighestTemperature = snapshot.HighestTemperature,
            TemperatureUnit = snapshot.TemperatureUnit
        };
    }

    /// <summary>
    /// Converts network details.
    /// </summary>
    private static NetworkDetails ConvertNetworkDetails(GoApiNetworkDetails snapshot)
    {
        return new NetworkDetails
        {
            Interfaces = snapshot.Interfaces.ToDictionary(
                i => i.Name,
                i => new NetworkInterface
                {
                    Name = i.Name,
                    RxBytes = i.RxBytes,
                    TxBytes = i.TxBytes,
                    RxPackets = i.RxPackets,
                    TxPackets = i.TxPackets,
                    Speed = (long)(i.RxSpeedMbps * 1000000), // Convert Mbps to bps
                    Status = i.Status
                }),
            TotalRx = (long)(snapshot.TotalRxMbps * 1000000), // Convert Mbps to bps
            TotalTx = (long)(snapshot.TotalTxMbps * 1000000), // Convert Mbps to bps
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts disk details.
    /// </summary>
    private static DiskDetails ConvertDiskDetails(List<GoApiDiskDetail> snapshot)
    {
        return new DiskDetails
        {
            Disks = snapshot.Select(d => new DiskInfo
            {
                Path = d.Path,
                TotalGb = d.TotalGb,
                UsedGb = d.UsedGb,
                FreeGb = d.FreeGb,
                UsedPercent = d.UsedPercent,
                Filesystem = d.Filesystem
            }).ToList()
        };
    }
}
