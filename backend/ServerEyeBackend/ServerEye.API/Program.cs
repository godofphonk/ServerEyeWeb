#pragma warning disable CA1303 // Localize strings

using ServerEye.API.Configuration.Extensions;
using ServerEye.API.Extensions;

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
app.MapHealthChecks("/health");

// Apply database migrations
app.ApplyDatabaseMigrations();

app.Run();

#pragma warning restore CA1303 // Localize strings
