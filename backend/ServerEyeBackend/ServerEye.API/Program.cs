#pragma warning disable CA1303 // Localize strings

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServerEye.API.Configuration.Extensions;
using ServerEye.API.Extensions;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure application settings
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container
builder.Services.AddControllers();

// Configure application layers
builder.Services
    .AddDatabaseConfiguration(builder.Configuration)
    .AddAuthenticationConfiguration(builder.Configuration)
    .AddCachingConfiguration(builder.Configuration)
    .AddMiddlewareConfiguration(builder.Configuration)
    .AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
app.UseMiddlewareConfiguration();
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

// Apply database migrations
await app.ApplyDatabaseMigrations();

await app.RunAsync();

#pragma warning restore CA1303 // Localize strings
