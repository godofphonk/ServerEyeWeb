namespace ServerEye.Infrastructure.ExternalServices.GoApi.Operations.Base;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Infrastructure.ExternalServices.GoApi;

/// <summary>
/// Base class for metrics operations with specialized logging and performance tracking.
/// </summary>
public abstract class MetricsOperation : GoApiOperation<GoApiMetricsResponse?>
{
    protected MetricsOperation(GoApiHttpHandler httpHandler, GoApiLogger logger)
        : base(httpHandler, logger)
    {
    }

    /// <summary>
    /// Processes metrics response with fallback to snapshot format.
    /// </summary>
    protected override GoApiMetricsResponse? ProcessResponse(string content)
    {
        // Try to parse as time series first
        var result = GoApiJsonSerializer.DeserializeMetricsResponse(content);

        // If no data points, try snapshot format
        if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
        {
            var snapshotResponse = GoApiJsonSerializer.DeserializeSnapshotResponse(content);
            if (snapshotResponse != null && snapshotResponse.Metrics != null)
            {
                result = GoApiDataTransformer.ConvertSnapshotToTimeSeries(snapshotResponse, GetStartTime(), GetEndTime(), GetGranularity());
            }
        }

        if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
        {
            Logger.LogEmptyData(GetOperationName(), GetServerIdentifier(), 0);
        }

        return result;
    }

    /// <summary>
    /// Logs performance metrics for operations.
    /// </summary>
    protected override bool ShouldLogPerformance(GoApiMetricsResponse? result)
    {
        return result != null && result.DataPoints != null;
    }

    /// <summary>
    /// Gets the total points count for performance logging.
    /// </summary>
    protected override int GetPerformanceMetric(GoApiMetricsResponse? result)
    {
        return result?.TotalPoints ?? 0;
    }

    /// <summary>
    /// Creates log data with points count.
    /// </summary>
    protected override object CreateLogData(GoApiMetricsResponse? result)
    {
        return new { Points = result?.TotalPoints ?? 0 };
    }

    /// <summary>
    /// Gets the start time for snapshot conversion.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract DateTime GetStartTime();

    /// <summary>
    /// Gets the end time for snapshot conversion.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract DateTime GetEndTime();

    /// <summary>
    /// Gets the granularity for snapshot conversion.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract string? GetGranularity();

    /// <summary>
    /// Gets the server identifier for logging.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract string GetServerIdentifier();
}
