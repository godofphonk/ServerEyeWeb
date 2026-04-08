#pragma warning disable CA1303 // Localize strings

using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServerEye.API.Configuration.Extensions;
using ServerEye.API.Extensions;
using ServerEye.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure application settings
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // Environment variables override JSON settings

// Add Doppler secrets for Production environment (from environment variables injected by doppler run)
if (builder.Environment.IsProduction())
{
    // Create logger manually to avoid BuildServiceProvider anti-pattern
    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var logger = loggerFactory.CreateLogger<Program>();
    
    builder.Configuration.AddDopplerSecretsFromEnvironment(logger);
}

// Add services to the container
builder.Services.AddControllers();

// Configure application layers
builder.Services
    .AddDatabaseConfiguration(builder.Configuration)
    .AddAuthenticationConfiguration(builder.Configuration)
    .AddCachingConfiguration(builder.Configuration)
    .AddMiddlewareConfiguration(builder.Configuration)
    .AddApplicationServices(builder.Configuration);

// Add OpenTelemetry only if not disabled
if (!builder.Configuration.GetValue("OpenTelemetry:DisableAllInstrumentation", false))
{
    builder.Services.AddOpenTelemetryConfiguration(builder.Configuration);
}

var app = builder.Build();

// Configure middleware pipeline
app.UseMiddlewareConfiguration();
app.UseContentSecurityPolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure Health Check endpoints with JSON response
var healthCheckOptions = new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
};

app.MapHealthChecks("/health", healthCheckOptions);
app.MapHealthChecks("/health/live", healthCheckOptions);
app.MapHealthChecks("/health/ready", healthCheckOptions);

// Map Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Apply database migrations (skip for testing environment)
var environment = app.Environment;
if (!environment.IsEnvironment("Testing"))
{
    await ServerEye.API.Configuration.DatabaseInitializer.InitializeAsync(app.Services);
}
await app.RunAsync();

#pragma warning restore CA1303 // Localize strings
