namespace ServerEye.Core.Services.Database;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// PostgreSQL health check implementation.
/// </summary>
public sealed class PostgreSQLHealthCheck : IHealthCheck
{
    private readonly ILogger<PostgreSQLHealthCheck> logger;
    private readonly PostgreSQLMonitoringOptions options;

    public PostgreSQLHealthCheck(
        ILogger<PostgreSQLHealthCheck> logger,
        IOptions<PostgreSQLMonitoringOptions> options)
    {
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(this.options.MainDatabaseConnectionString))
            {
                return HealthCheckResult.Unhealthy("PostgreSQL connection string not configured");
            }

            await using var connection = new NpgsqlConnection(this.options.MainDatabaseConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("PostgreSQL is healthy");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "PostgreSQL health check failed");
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed", ex);
        }
    }
}
