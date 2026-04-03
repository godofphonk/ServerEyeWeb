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
/// </summary>
public class DatabaseHealthCheckFactory<TContext> : IHealthCheck
    where TContext : DbContext
{
    private readonly IServiceProvider serviceProvider;
    private readonly string databaseName;

    public DatabaseHealthCheckFactory(IServiceProvider serviceProvider, string databaseName)
    {
        this.serviceProvider = serviceProvider;
        this.databaseName = databaseName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var healthCheck = new DatabaseHealthCheck<TContext>(dbContext, databaseName);
        return await healthCheck.CheckHealthAsync(context, cancellationToken);
    }
}
