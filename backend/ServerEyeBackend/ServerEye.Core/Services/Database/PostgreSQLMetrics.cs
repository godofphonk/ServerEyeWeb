namespace ServerEye.Core.Services.Database;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

/// <summary>
/// PostgreSQL performance metrics collection.
/// </summary>
public sealed class PostgreSQLMetrics : IDisposable
{
    private readonly ILogger<PostgreSQLMetrics> logger;
    private readonly Meter meter;
    
    // Connection pool metrics
    private readonly Counter<long> connectionPoolAcquisitions;
    private readonly Counter<long> connectionPoolReleases;
    private readonly Counter<long> connectionPoolTimeouts;
    private readonly Histogram<double> connectionAcquisitionDuration;
    
    // Query performance metrics
    private readonly Histogram<double> queryDuration;
    private readonly Counter<long> queryExecutions;
    private readonly Counter<long> queryFailures;
    private readonly Counter<long> slowQueries;
    
    // Database health metrics
    private readonly Counter<long> deadlocks;
    private readonly Counter<long> lockTimeouts;
    private readonly Histogram<double> transactionDuration;
    
    private int activeConnectionsCount;
    private long currentDatabaseSize;
    private double currentCacheHitRatio;

    public PostgreSQLMetrics(IMeterFactory meterFactory, ILogger<PostgreSQLMetrics> logger)
    {
        this.logger = logger;
        this.meter = meterFactory.Create("ServerEye.PostgreSQL");
        
        // Initialize connection pool metrics
        this.connectionPoolAcquisitions = this.meter.CreateCounter<long>(
            "postgres_connection_pool_acquisitions_total",
            "Total number of connection pool acquisitions");
            
        this.connectionPoolReleases = this.meter.CreateCounter<long>(
            "postgres_connection_pool_releases_total", 
            "Total number of connection pool releases");
            
        this.connectionPoolTimeouts = this.meter.CreateCounter<long>(
            "postgres_connection_pool_timeouts_total",
            "Total number of connection pool timeouts");
            
        this.connectionAcquisitionDuration = this.meter.CreateHistogram<double>(
            "postgres_connection_acquisition_duration_seconds",
            "Duration of connection acquisition in seconds");
        
        // Initialize query performance metrics
        this.queryDuration = this.meter.CreateHistogram<double>(
            "postgres_query_duration_seconds",
            "Duration of database queries in seconds");
            
        this.queryExecutions = this.meter.CreateCounter<long>(
            "postgres_queries_executed_total",
            "Total number of executed queries");
            
        this.queryFailures = this.meter.CreateCounter<long>(
            "postgres_query_failures_total",
            "Total number of failed queries");
            
        this.slowQueries = this.meter.CreateCounter<long>(
            "postgres_slow_queries_total",
            "Total number of slow queries (>1s)");
        
        // Initialize database health metrics
        this.deadlocks = this.meter.CreateCounter<long>(
            "postgres_deadlocks_total",
            "Total number of database deadlocks");
            
        this.lockTimeouts = this.meter.CreateCounter<long>(
            "postgres_lock_timeouts_total",
            "Total number of lock timeouts");
            
        this.transactionDuration = this.meter.CreateHistogram<double>(
            "postgres_transaction_duration_seconds",
            "Duration of database transactions in seconds");
        
        this.logger.LogInformation("PostgreSQL metrics initialized successfully");
    }

    /// <summary>
    /// Record connection pool acquisition.
    /// </summary>
    public void RecordConnectionAcquisition(double duration, bool success)
    {
        var tags = new TagList
        {
            { "status", success ? "success" : "timeout" }
        };
        
        if (success)
        {
            this.connectionPoolAcquisitions.Add(1, tags);
            this.connectionAcquisitionDuration.Record(duration, tags);
            this.activeConnectionsCount++;
        }
        else
        {
            this.connectionPoolTimeouts.Add(1, tags);
            this.logger.LogWarning("Connection pool timeout after {Duration}s", duration);
        }
    }

    /// <summary>
    /// Record connection pool release.
    /// </summary>
    public void RecordConnectionRelease()
    {
        this.connectionPoolReleases.Add(1);
        this.activeConnectionsCount = Math.Max(0, this.activeConnectionsCount - 1);
    }

    /// <summary>
    /// Record query execution.
    /// </summary>
    public void RecordQueryExecution(string queryType, double duration, bool success, string? errorMessage = null)
    {
        var tags = new TagList
        {
            { "query_type", queryType },
            { "status", success ? "success" : "error" }
        };
        
        this.queryDuration.Record(duration, tags);
        
        if (success)
        {
            this.queryExecutions.Add(1, tags);
            
            // Record slow queries (>1 second)
            if (duration > 1.0)
            {
                this.slowQueries.Add(1, tags);
                this.logger.LogWarning("Slow query detected - Type: {QueryType}, Duration: {Duration}s", queryType, duration);
            }
        }
        else
        {
            this.queryFailures.Add(1, tags);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                tags.Add("error_message", errorMessage);
            }
            this.logger.LogError("Query failed - Type: {QueryType}, Error: {Error}", queryType, errorMessage);
        }
    }

    /// <summary>
    /// Record transaction execution.
    /// </summary>
    public void RecordTransactionExecution(double duration, bool success, bool? wasDeadlocked = null)
    {
        var tags = new TagList
        {
            { "status", success ? "success" : "error" }
        };
        
        this.transactionDuration.Record(duration, tags);
        
        if (wasDeadlocked == true)
        {
            this.deadlocks.Add(1);
            this.logger.LogWarning("Deadlock detected - Duration: {Duration}s", duration);
        }
        
        if (!success)
        {
            this.lockTimeouts.Add(1, tags);
            this.logger.LogError("Transaction failed - Duration: {Duration}s", duration);
        }
    }

    /// <summary>
    /// Update database statistics.
    /// </summary>
    public void UpdateDatabaseStatistics(long sizeBytes, double cacheHitRatio)
    {
        this.currentDatabaseSize = sizeBytes;
        this.currentCacheHitRatio = cacheHitRatio;
        
        this.logger.LogDebug(
            "Database statistics updated - Size: {Size}MB, Cache Hit Ratio: {CacheHitRatio:P2}",
            this.currentDatabaseSize / 1024 / 1024,
            this.currentCacheHitRatio);
    }

    /// <summary>
    /// Update connection pool statistics.
    /// </summary>
    public void UpdateConnectionPoolStatistics(int active)
    {
        this.activeConnectionsCount = active;
    }

    public void Dispose()
    {
        this.meter?.Dispose();
        this.logger.LogInformation("PostgreSQL metrics disposed");
    }
}
