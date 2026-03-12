namespace ServerEye.API.Configuration.Extensions;

using Microsoft.EntityFrameworkCore;
using ServerEye.Infrastructure;

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

        // Register DbContexts
        services.AddDbContext<ServerEyeDbContext>(options =>
            options.UseNpgsql(serverEyeConnectionString));

        services.AddDbContext<TicketDbContext>(options =>
            options.UseNpgsql(ticketConnectionString));

        // Add Health Checks
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: serverEyeConnectionString,
                name: "postgres-servereye",
                tags: ["db", "postgres", "ready"])
            .AddNpgSql(
                connectionString: ticketConnectionString,
                name: "postgres-tickets",
                tags: ["db", "postgres", "ready"]);

        return services;
    }

    /// <summary>
    /// Applies database migrations.
    /// </summary>
    public static IApplicationBuilder ApplyDatabaseMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var serverEyeContext = services.GetRequiredService<ServerEyeDbContext>();
            serverEyeContext.Database.Migrate();

            var ticketContext = services.GetRequiredService<TicketDbContext>();
            ticketContext.Database.Migrate();
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

        return app;
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
            ?? throw new InvalidOperationException("Ticket database connection string not found");
    }
}
