namespace ServerEye.API.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServerEye.Infrastructure;
using ServerEye.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting database initialization...");

            // Apply Main DB migrations
            await ApplyMigrationsAsync<ServerEyeDbContext>(scope.ServiceProvider, logger, "Main Database");

            // Apply Billing DB migrations
            await ApplyMigrationsAsync<BillingDbContext>(scope.ServiceProvider, logger, "Billing Database");

            // Apply Ticket DB migrations
            await ApplyMigrationsAsync<TicketDbContext>(scope.ServiceProvider, logger, "Ticket Database");

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing databases");
            throw;
        }
    }

    private static async Task ApplyMigrationsAsync<TContext>(
        IServiceProvider serviceProvider,
        ILogger logger,
        string databaseName)
        where TContext : DbContext
    {
        try
        {
            logger.LogInformation("Applying migrations for {DatabaseName}...", databaseName);

            var context = serviceProvider.GetRequiredService<TContext>();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var pendingCount = pendingMigrations.Count();

            if (pendingCount > 0)
            {
                logger.LogInformation("{DatabaseName}: Found {Count} pending migrations", databaseName, pendingCount);

                // Apply migrations
                await context.Database.MigrateAsync();

                logger.LogInformation("{DatabaseName}: Migrations applied successfully", databaseName);
            }
            else
            {
                logger.LogInformation("{DatabaseName}: No pending migrations", databaseName);
            }

            // Verify connection
            var canConnect = await context.Database.CanConnectAsync();
            if (canConnect)
            {
                logger.LogInformation("{DatabaseName}: Connection verified", databaseName);
            }
            else
            {
                logger.LogWarning("{DatabaseName}: Cannot connect to database", databaseName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying migrations for {DatabaseName}", databaseName);
            throw;
        }
    }
}
