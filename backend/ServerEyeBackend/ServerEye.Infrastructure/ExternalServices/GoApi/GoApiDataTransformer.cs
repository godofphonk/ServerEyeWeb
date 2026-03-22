namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using System.Globalization;

/// <summary>
/// Data transformation and conversion for Go API responses.
/// </summary>
public static class GoApiDataTransformer
{
    /// <summary>
    /// Converts snapshot response to time series format.
    /// </summary>
    public static GoApiMetricsResponse? ConvertSnapshotToTimeSeries(GoApiSnapshotResponse snapshot, DateTime start, DateTime end, string? granularity)
    {
        var dataPoints = GenerateDataPointsFromSnapshot(snapshot);
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
            Kernel = response.ServerInfo.Kernel,
            Architecture = response.ServerInfo.Architecture,
            AgentVersion = string.Empty, // Will be populated from metrics endpoint
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
            MemoryInfo = response.HardwareInfo != null && response.HardwareInfo.TotalMemoryGb > 0
                ? new StaticMemoryInfo
                {
                    TotalGb = response.HardwareInfo.TotalMemoryGb,
                    Type = response.MemoryModules.FirstOrDefault()?.MemoryType ?? "Unknown",
                    SpeedMhz = response.MemoryModules.FirstOrDefault()?.FrequencyMhz ?? 0
                }
                : null,
            MotherboardInfo = response.MotherboardInfo != null && !string.IsNullOrEmpty(response.MotherboardInfo.Model)
                ? new StaticMotherboardInfo
                {
                    Manufacturer = response.MotherboardInfo.Manufacturer,
                    Model = response.MotherboardInfo.Model,
                    BiosDate = response.MotherboardInfo.BiosDate != default ? response.MotherboardInfo.BiosDate : null
                }
                : null,
            GpuInfo = response.HardwareInfo != null && !string.IsNullOrEmpty(response.HardwareInfo.GpuModel)
                ? new StaticGpuInfo
                {
                    Model = response.HardwareInfo.GpuModel,
                    Driver = response.HardwareInfo.GpuDriver,
                    MemoryGb = response.HardwareInfo.GpuMemoryGb
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
    /// NOTE: This is a fallback for when historical data is not available.
    /// It creates a single data point from the current snapshot.
    /// </summary>
    private static List<GoApiDataPoint> GenerateDataPointsFromSnapshot(GoApiSnapshotResponse snapshot)
    {
        var dataPoints = new List<GoApiDataPoint>();
        
        // Create a single data point from current snapshot
        // This is NOT historical data - just current metrics formatted as a data point
        dataPoints.Add(new GoApiDataPoint
        {
            Timestamp = snapshot.Timestamp,
            CpuAvg = snapshot.Metrics.Cpu,
            CpuMax = snapshot.Metrics.Cpu,
            CpuMin = snapshot.Metrics.Cpu,
            MemoryAvg = snapshot.Metrics.Memory,
            MemoryMax = snapshot.Metrics.Memory,
            MemoryMin = snapshot.Metrics.Memory,
            DiskAvg = snapshot.Metrics.Disk,
            DiskMax = snapshot.Metrics.Disk,
            NetworkAvg = snapshot.Metrics.Network,
            NetworkMax = snapshot.Metrics.Network,
            TempAvg = snapshot.Metrics.TemperatureDetails.CpuTemperature,
            TempMax = snapshot.Metrics.TemperatureDetails.HighestTemperature,
            LoadAvg = snapshot.Metrics.CpuUsage.LoadAverage.Load1Min,
            LoadMax = snapshot.Metrics.CpuUsage.LoadAverage.Load1Min,
            SampleCount = 1
        });

        return dataPoints;
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
