namespace ServerEye.API.Configuration.Extensions;

using Microsoft.EntityFrameworkCore;
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

        // Register DbContexts
        services.AddDbContext<ServerEyeDbContext>(options =>
            options.UseNpgsql(serverEyeConnectionString));

        services.AddDbContext<TicketDbContext>(options =>
            options.UseNpgsql(ticketConnectionString));

        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(billingConnectionString));

        // Add Health Checks
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

        try
        {
            var serverEyeContext = services.GetRequiredService<ServerEyeDbContext>();

            // Skip migrations for existing database
            // await serverEyeContext.Database.MigrateAsync();
            var ticketContext = services.GetRequiredService<TicketDbContext>();

            // Skip migrations for existing database
            // await ticketContext.Database.MigrateAsync();
            // Billing plans are now hardcoded in SubscriptionService, no DB needed
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Critical error: Failed to apply database migrations. Application cannot continue.");

            // In production, we want the application to fail fast instead of running with broken database
            if (!app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                throw new InvalidOperationException("Database migration failed. Application startup terminated.", ex);
            }
        }
    }

    private static string GetServerEyeConnectionString(IConfiguration configuration)
    {
        return configuration["DATABASE_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("ServerEyeDbContext")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ServerEye database connection string not found");
    }

    private static string GetTicketConnectionString(IConfiguration configuration)
    {
        return configuration["TICKET_DB_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("TicketDbContext")
            ?? configuration.GetConnectionString("TicketConnection")
            ?? throw new InvalidOperationException("Ticket database connection string not found");
    }

    private static string GetBillingConnectionString(IConfiguration configuration)
    {
        return configuration["BILLING_DB_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("BillingDbContext")
            ?? configuration.GetConnectionString("BillingConnection")
            ?? throw new InvalidOperationException("Billing database connection string not found");
    }
}
