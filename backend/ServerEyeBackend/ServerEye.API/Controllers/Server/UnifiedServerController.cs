namespace ServerEye.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerEye.API.DTOs;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Interfaces.Services;

/// <summary>
/// Unified controller for server metrics, status, and static info.
/// </summary>
[ApiController]
[Route("api/servers/by-key/{serverKey}")]
[Authorize]
public class UnifiedServerController : ControllerBase
{
    private readonly IGoApiClient goApiClient;
    private readonly ILogger<UnifiedServerController> logger;

    public UnifiedServerController(
        IGoApiClient goApiClient,
        ILogger<UnifiedServerController> logger)
    {
        this.goApiClient = goApiClient;
        this.logger = logger;
    }

    /// <summary>
    /// Get unified server data combining metrics, status, and static info.
    /// </summary>
    /// <param name="serverKey">Server key identifier.</param>
    /// <param name="request">Unified request parameters.</param>
    /// <returns>Combined server data.</returns>
    [HttpGet("unified")]
    public async Task<ActionResult<GoApiUnifiedResponse>> GetUnifiedData(
        string serverKey,
        [FromQuery] UnifiedRequest request)
    {
        try
        {
            this.logger.LogInformation(
                "GetUnifiedData called: ServerKey={ServerKey}, IncludeMetrics={IncludeMetrics}, IncludeStatus={IncludeStatus}, IncludeStatic={IncludeStatic}",
                serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null",
                request.IncludeMetrics,
                request.IncludeStatus,
                request.IncludeStatic);

            // Proxy request to Go API unified endpoint
            var response = await this.goApiClient.GetUnifiedMetricsAsync(
                serverKey ?? string.Empty,
                request.IncludeMetrics,
                request.IncludeStatus,
                request.IncludeStatic);

            if (response == null)
            {
                return NotFound("Server not found or no data available");
            }

            this.logger.LogInformation("GetUnifiedData completed successfully for server {ServerKey}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogWarning("Unauthorized access attempt for server key {ServerKey}: {Message}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", ex.Message);
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in GetUnifiedData for server key {ServerKey}", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
            return StatusCode(500, "Internal server error");
        }
    }
}
