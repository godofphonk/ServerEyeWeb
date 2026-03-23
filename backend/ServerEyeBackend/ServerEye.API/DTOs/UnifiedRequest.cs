namespace ServerEye.API.DTOs;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Request parameters for unified endpoint.
/// </summary>
public class UnifiedRequest
{
    /// <summary>
    /// Include current metrics data (default: true).
    /// </summary>
    [FromQuery(Name = "include_metrics")]
    public bool IncludeMetrics { get; set; } = true;

    /// <summary>
    /// Include server status (default: true).
    /// </summary>
    [FromQuery(Name = "include_status")]
    public bool IncludeStatus { get; set; } = true;

    /// <summary>
    /// Include static information (default: true).
    /// </summary>
    [FromQuery(Name = "include_static")]
    public bool IncludeStatic { get; set; } = true;

    /// <summary>
    /// Time range for metrics (optional).
    /// </summary>
    [FromQuery(Name = "start")]
    public DateTime? Start { get; set; }

    /// <summary>
    /// End time for metrics (optional).
    /// </summary>
    [FromQuery(Name = "end")]
    public DateTime? End { get; set; }

    /// <summary>
    /// Granularity for metrics (optional).
    /// </summary>
    [FromQuery(Name = "granularity")]
    public string? Granularity { get; set; }
}
