namespace ServerEye.Core.Services;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Entities;

public static class MetricsMapper
{
    public static MetricsResponse MapToResponse(GoApiMetricsResponse goResponse, Server server, bool isCached)
    {
        var dataPoints = goResponse.DataPoints?.Select(MapDataPoint).ToList() ?? new List<DataPoint>();

        return new MetricsResponse
        {
            ServerId = goResponse.ServerId,
            ServerName = server.Hostname,
            StartTime = goResponse.StartTime,
            EndTime = goResponse.EndTime,
            Granularity = goResponse.Granularity,
            DataPoints = dataPoints,
            TotalPoints = goResponse.TotalPoints,
            Summary = CalculateSummary(goResponse.DataPoints ?? new List<GoApiDataPoint>(), goResponse.StartTime, goResponse.EndTime),
            IsCached = isCached,
            CachedAt = isCached ? DateTime.UtcNow : null
        };
    }

    public static DataPoint MapDataPoint(GoApiDataPoint goPoint)
    {
        return new DataPoint
        {
            Timestamp = goPoint.Timestamp,
            Cpu = new MetricValue
            {
                Avg = goPoint.CpuAvg,
                Max = goPoint.CpuMax,
                Min = goPoint.CpuMin
            },
            Memory = new MetricValue
            {
                Avg = goPoint.MemoryAvg,
                Max = goPoint.MemoryMax,
                Min = goPoint.MemoryMin
            },
            Disk = new MetricValue
            {
                Avg = goPoint.DiskAvg,
                Max = goPoint.DiskMax,
                Min = goPoint.DiskAvg
            },
            Network = new MetricValue
            {
                Avg = goPoint.NetworkAvg,
                Max = goPoint.NetworkMax,
                Min = goPoint.NetworkAvg
            },
            Temperature = new MetricValue
            {
                Avg = goPoint.TempAvg,
                Max = goPoint.TempMax,
                Min = goPoint.TempAvg
            },
            LoadAverage = new MetricValue
            {
                Avg = goPoint.LoadAvg,
                Max = goPoint.LoadMax,
                Min = goPoint.LoadAvg
            },
            SampleCount = goPoint.SampleCount
        };
    }

    public static MetricsSummary CalculateSummary(List<GoApiDataPoint> dataPoints, DateTime startTime, DateTime endTime)
    {
        if (dataPoints.Count == 0)
        {
            return new MetricsSummary
            {
                TimeRange = endTime - startTime
            };
        }

        return new MetricsSummary
        {
            AvgCpu = dataPoints.Average(d => d.CpuAvg),
            MaxCpu = dataPoints.Max(d => d.CpuMax),
            MinCpu = dataPoints.Min(d => d.CpuMin),
            AvgMemory = dataPoints.Average(d => d.MemoryAvg),
            MaxMemory = dataPoints.Max(d => d.MemoryMax),
            MinMemory = dataPoints.Min(d => d.MemoryMin),
            AvgDisk = dataPoints.Average(d => d.DiskAvg),
            MaxDisk = dataPoints.Max(d => d.DiskMax),
            TotalDataPoints = dataPoints.Count,
            TimeRange = endTime - startTime
        };
    }
}
