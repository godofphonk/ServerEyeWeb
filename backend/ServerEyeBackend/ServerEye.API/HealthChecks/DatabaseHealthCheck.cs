namespace ServerEye.API.HealthChecks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServerEye.Infrastructure;
using ServerEye.Infrastructure.Data;

public class DatabaseHealthCheck<TContext> : IHealthCheck
    where TContext : DbContext
{
    private readonly TContext context;
    private readonly string databaseName;

    public DatabaseHealthCheck(TContext context, string databaseName)
    {
        this.context = context;
        this.databaseName = databaseName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await this.context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(
                    $"{databaseName}: Cannot connect to database",
                    data: new Dictionary<string, object>
                    {
                        ["database"] = databaseName,
                        ["status"] = "disconnected"
                    });
            }

            var pendingMigrations = await this.context.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingCount = pendingMigrations.Count();

            if (pendingCount > 0)
            {
                return HealthCheckResult.Degraded(
                    $"{databaseName}: {pendingCount} pending migrations",
                    data: new Dictionary<string, object>
                    {
                        ["database"] = databaseName,
                        ["pendingMigrations"] = pendingCount,
                        ["migrations"] = pendingMigrations.ToArray()
                    });
            }

            return HealthCheckResult.Healthy(
                $"{databaseName}: Connected and up to date",
                data: new Dictionary<string, object>
                {
                    ["database"] = databaseName,
                    ["status"] = "healthy"
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"{databaseName}: Health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["database"] = databaseName,
                    ["error"] = ex.Message
                });
        }
    }
}

/// <summary>
/// Factory for creating DatabaseHealthCheck instances with database name.
/// Optimized to reuse scoped DbContext and cache results.
/// </summary>
public class DatabaseHealthCheckFactory<TContext> : IHealthCheck
    where TContext : DbContext
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly string databaseName;
    private HealthCheckResult? cachedResult;
    private DateTime lastCheckTime = DateTime.MinValue;

    public DatabaseHealthCheckFactory(IServiceScopeFactory scopeFactory, string databaseName)
    {
        this.scopeFactory = scopeFactory;
        this.databaseName = databaseName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Return cached result if still valid
        if (cachedResult != null && DateTime.UtcNow - lastCheckTime < CacheDuration)
        {
            return cachedResult.Value;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var healthCheck = new DatabaseHealthCheck<TContext>(dbContext, databaseName);
        var result = await healthCheck.CheckHealthAsync(context, cancellationToken);

        // Cache the result
        cachedResult = result;
        lastCheckTime = DateTime.UtcNow;

        return result;
    }
}
