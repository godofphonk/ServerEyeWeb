namespace ServerEye.Core.Services.Database;

/// <summary>
/// Configuration options for PostgreSQL monitoring.
/// </summary>
public sealed class PostgreSQLMonitoringOptions
{
    public string MainDatabaseConnectionString { get; set; } = string.Empty;
    public string TicketsDatabaseConnectionString { get; set; } = string.Empty;
    public string BillingDatabaseConnectionString { get; set; } = string.Empty;
    public int MetricsUpdateIntervalSeconds { get; set; } = 30;
}
