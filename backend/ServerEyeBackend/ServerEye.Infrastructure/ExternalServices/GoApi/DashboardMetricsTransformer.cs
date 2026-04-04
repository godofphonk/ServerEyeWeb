namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;

/// <summary>
/// Transforms RawMetricsResponse to DashboardMetricsResponse for frontend consumption.
/// </summary>
public static class DashboardMetricsTransformer
{
    /// <summary>
    /// Transforms RawMetricsResponse to DashboardMetricsResponse.
    /// </summary>
    public static DashboardMetricsResponse TransformToDashboardMetrics(RawMetricsResponse rawResponse)
    {
        var current = GetCurrentMetrics(rawResponse);
        var trends = CalculateTrends(rawResponse);

        return new DashboardMetricsResponse
        {
            ServerId = rawResponse.ServerId,
            ServerName = rawResponse.ServerName,
            Current = current,
            Trends = trends,
            IsCached = rawResponse.IsCached,
            CachedAt = rawResponse.CachedAt,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Extracts current metrics from the latest data point.
    /// </summary>
    private static CurrentMetrics GetCurrentMetrics(RawMetricsResponse rawResponse)
    {
        var latestPoint = rawResponse.DataPoints?.LastOrDefault();

        if (latestPoint != null)
        {
            return new CurrentMetrics
            {
                Cpu = latestPoint.CpuAvg,
                Memory = latestPoint.MemoryAvg,
                Disk = latestPoint.DiskAvg,
                Network = latestPoint.NetworkAvg,
                Temperature = latestPoint.TempAvg,
                Load = latestPoint.LoadAvg,

                // Extract memory details from data point
                MemoryCache = latestPoint.MemoryCacheGb,
                MemoryBuffers = latestPoint.MemoryBuffersGb,
                MemoryAvailable = latestPoint.MemoryAvailableGb,
                MemorySwap = latestPoint.MemorySwapPercent,

                // Extract disk I/O metrics from data point
                DiskReadSpeed = latestPoint.DiskReadMb,
                DiskWriteSpeed = latestPoint.DiskWriteMb
            };
        }

        // Fallback: return zeros if no data available
        return new CurrentMetrics
        {
            Cpu = 0,
            Memory = 0,
            Disk = 0,
            Network = 0,
            Temperature = rawResponse.TemperatureDetails?.CpuTemperature ?? 0,
            Load = 0,

            // Memory details fallback to zeros
            MemoryCache = 0,
            MemoryBuffers = 0,
            MemoryAvailable = 0,
            MemorySwap = 0,

            // Disk I/O metrics fallback to zeros
            DiskReadSpeed = 0,
            DiskWriteSpeed = 0
        };
    }

    /// <summary>
    /// Calculates trends based on data points.
    /// </summary>
    private static MetricTrends CalculateTrends(RawMetricsResponse rawResponse)
    {
        var dataPoints = rawResponse.DataPoints?.ToList() ?? new List<GoApiDataPoint>();

        if (dataPoints.Count < 2)
        {
            return new MetricTrends(); // No trend data
        }

        var first = dataPoints.First();
        var last = dataPoints.Last();

        return new MetricTrends
        {
            Cpu = CalculateTrend(first.CpuAvg, last.CpuAvg),
            Memory = CalculateTrend(first.MemoryAvg, last.MemoryAvg),
            Disk = CalculateTrend(first.DiskAvg, last.DiskAvg),
            Network = CalculateTrend(first.NetworkAvg, last.NetworkAvg),
            Temperature = CalculateTrend(first.TempAvg, last.TempAvg),
            Load = CalculateTrend(first.LoadAvg, last.LoadAvg)
        };
    }

    /// <summary>
    /// Calculates percentage trend between two values.
    /// </summary>
    private static double CalculateTrend(double oldValue, double newValue)
    {
        if (oldValue == 0)
        {
            return 0;
        }

        var change = newValue - oldValue;
        return Math.Round(change / oldValue * 100, 2);
    }
}
