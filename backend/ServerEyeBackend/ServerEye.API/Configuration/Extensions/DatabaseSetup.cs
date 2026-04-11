namespace ServerEye.API.Configuration.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServerEye.Infrastructure;
using ServerEye.Infrastructure.Data;

/// <summary>
/// Database configuration and health checks setup.
/// </summary>
public static class DatabaseSetup
{
    /// <summary>
    /// Adds database contexts and health checks.
    /// </summary>
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serverEyeConnectionString = GetServerEyeConnectionString(configuration);
        var ticketConnectionString = GetTicketConnectionString(configuration);
        var billingConnectionString = GetBillingConnectionString(configuration);

        // If connection strings are missing (testing environment), skip registration
        // TestApplicationFactory will override with in-memory databases
        if (serverEyeConnectionString == null || ticketConnectionString == null || billingConnectionString == null)
        {
            return services;
        }

        // Register DbContexts
        services.AddDbContext<ServerEyeDbContext>(options =>
            options.UseNpgsql(serverEyeConnectionString));

        services.AddDbContext<TicketDbContext>(options =>
            options.UseNpgsql(ticketConnectionString));

        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(billingConnectionString));

        // Add Health Checks for all databases
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: serverEyeConnectionString,
                name: "postgres-servereye",
                tags: ["db", "postgres", "ready"])
            .AddNpgSql(
                connectionString: ticketConnectionString,
                name: "postgres-tickets",
                tags: ["db", "postgres", "ready"])
            .AddNpgSql(
                connectionString: billingConnectionString,
                name: "postgres-billing",
                tags: ["db", "postgres", "ready"]);

        return services;
    }

    /// <summary>
    /// Applies database migrations.
    /// </summary>
    public static async Task ApplyDatabaseMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var serverEyeContext = services.GetRequiredService<ServerEyeDbContext>();

            // Ensure database exists without applying migrations
            logger.LogInformation("Ensuring database exists...");
            await serverEyeContext.Database.EnsureCreatedAsync();
            logger.LogInformation("Database ensured successfully");

            // Billing plans are now hardcoded in SubscriptionService, no DB needed
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error: Failed to apply database migrations. Application cannot continue.");

            // In production, we want the application to fail fast instead of running with broken database
            if (!app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                throw new InvalidOperationException("Database migration failed. Application startup terminated.", ex);
            }
        }
    }

    private static string? GetServerEyeConnectionString(IConfiguration configuration)
    {
        return configuration["DATABASE_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("ServerEyeDbContext")
            ?? configuration.GetConnectionString("DefaultConnection");
    }

    private static string? GetTicketConnectionString(IConfiguration configuration)
    {
        return configuration["TICKET_DB_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("TicketDbContext")
            ?? configuration.GetConnectionString("TicketConnection");
    }

    private static string? GetBillingConnectionString(IConfiguration configuration)
    {
        return configuration["BILLING_DB_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("BillingDbContext")
            ?? configuration.GetConnectionString("BillingConnection");
    }
}
