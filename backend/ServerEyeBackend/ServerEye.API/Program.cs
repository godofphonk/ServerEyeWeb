#pragma warning disable CA1303 // Localize strings

using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Tokens;
using ServerEye.API.Configuration;
using ServerEye.API.Controllers;
using ServerEye.API.Validators;
using ServerEye.Core.Configuration;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services;
using ServerEye.Infrastracture;
using ServerEye.Infrastracture.Repositories;
using ServerEye.API.Extensions;
using System.Text;
using System.Linq;

// Helper function to build connection string from environment variables
static string BuildConnectionStringFromEnvironment(IConfiguration configuration)
{
    var host = configuration["DATABASE_HOST"] ?? "localhost";
    var port = configuration["DATABASE_PORT"] ?? "5432";
    var database = configuration["DATABASE_NAME"] ?? "ServerEyeWeb";
    var username = configuration["DATABASE_USER"] ?? "postgres";
    var password = configuration["DATABASE_PASSWORD"] ?? "postgres";
    
    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Disable";
}

var builder = WebApplication.CreateBuilder(args);

// Configure logging early
using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
var startupLogger = loggerFactory.CreateLogger("Startup");

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Debug: Log all environment variables
startupLogger.LogInformation("Loading environment variables");
foreach (var envVar in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>())
{
    var key = envVar.Key?.ToString();
    var value = envVar.Value?.ToString();
    if (key != null && (key.Contains("DATABASE", StringComparison.OrdinalIgnoreCase) || key.Contains("REDIS", StringComparison.OrdinalIgnoreCase)))
    {
        startupLogger.LogDebug("Environment variable: {Key} = {Value}", key, value);
    }
}

// Add Doppler configuration for production environments
if (builder.Environment.IsProduction() || builder.Environment.IsStaging() || builder.Environment.IsDevelopment())
{
    var dopplerProject = Environment.GetEnvironmentVariable("DOPPLER_PROJECT") ?? "servereye";
    var dopplerConfig = Environment.GetEnvironmentVariable("DOPPLER_CONFIG") ?? builder.Environment.EnvironmentName;

    startupLogger.LogInformation("Loading secrets from Doppler - Project: {Project}, Config: {Config}", dopplerProject, dopplerConfig);

    try
    {
        builder.Configuration.AddDopplerSecrets(dopplerProject, dopplerConfig, startupLogger);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Failed to load Doppler secrets. Falling back to environment variables and appsettings");
    }
}

var configuration = builder.Configuration;

// Configure settings
var jwtSettings = configuration.GetSection("JwtSettings").Get<ServerEye.Core.Services.JwtSettings>() ?? new ServerEye.Core.Services.JwtSettings();

// Load RSA keys from environment variables (Doppler)
jwtSettings.PrivateKeyBase64 = configuration["JWT_PRIVATE_KEY_BASE64"] ?? string.Empty;
jwtSettings.PublicKeyBase64 = configuration["JWT_PUBLIC_KEY_BASE64"] ?? string.Empty;

startupLogger.LogInformation("JWT Private Key Loaded: {IsLoaded}", !string.IsNullOrEmpty(jwtSettings.PrivateKeyBase64));
startupLogger.LogInformation("JWT Public Key Loaded: {IsLoaded}", !string.IsNullOrEmpty(jwtSettings.PublicKeyBase64));
var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>() ?? new SecuritySettings();
var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
var goApiSettings = configuration.GetSection("GoApiSettings").Get<GoApiSettings>() ?? new GoApiSettings();
var cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();
var redisSettings = configuration.GetSection("Redis").Get<RedisSettings>() ?? new RedisSettings();
var emailSettings = configuration.GetSection("EmailSettings").Get<ServerEye.Core.Configuration.EmailSettings>() ?? new ServerEye.Core.Configuration.EmailSettings();

// Configure Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "application/xml", "text/plain", "text/json"]);
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Configure CORS with settings
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(corsSettings.AllowedOrigins)
              .WithMethods(corsSettings.AllowedMethods)
              .WithHeaders(corsSettings.AllowedHeaders)
              .AllowCredentials());
});

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Use RSA key for validation - must match the key used in JwtService
    var rsa = System.Security.Cryptography.RSA.Create();

    // In production, load the public key from secure storage
    // For now, we'll use the same static key from JwtService
    var rsaKey = ServerEye.Core.Services.JwtService.GetStaticRsaKey;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new RsaSecurityKey(rsaKey),
        ClockSkew = TimeSpan.Zero
    };

    // Enable proper token validation
    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    options.TokenValidationParameters.RequireSignedTokens = true;
});

builder.Services.AddAuthorization();

// Configure Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["REDIS_CONNECTION_STRING"] ?? redisSettings.ConnectionString;
    options.InstanceName = redisSettings.InstanceName;
});

// Register settings as singletons
builder.Services.AddSingleton(goApiSettings);
builder.Services.AddSingleton(cacheSettings);
builder.Services.AddSingleton(emailSettings);

// Configure HttpClient for Go API (endpoints are public, no API key needed)
builder.Services.AddHttpClient<IGoApiClient, ServerEye.Infrastracture.ExternalServices.GoApiClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<ServerEye.Core.Configuration.GoApiSettings>();
    client.BaseAddress = settings.BaseUrl;
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

// Register repositories
builder.Services.AddScoped<IServerRepository, ServerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IMonitoredServerRepository, ServerEye.Infrastracture.Repositories.MonitoredServerRepository>();
builder.Services.AddScoped<IUserServerAccessRepository, ServerEye.Infrastracture.Repositories.UserServerAccessRepository>();
builder.Services.AddScoped<ITicketRepository, ServerEye.Infrastracture.Repositories.TicketRepository>();
builder.Services.AddScoped<ITicketMessageRepository, ServerEye.Infrastracture.Repositories.TicketMessageRepository>();
builder.Services.AddScoped<INotificationRepository, ServerEye.Infrastracture.Repositories.NotificationRepository>();
builder.Services.AddScoped<IEmailVerificationRepository, ServerEye.Infrastracture.Repositories.EmailVerificationRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, ServerEye.Infrastracture.Repositories.PasswordResetTokenRepository>();
builder.Services.AddScoped<IAccountDeletionRepository, ServerEye.Infrastracture.Repositories.AccountDeletionRepository>();

// Register services
builder.Services.AddScoped<IEmailTemplateService, ServerEye.API.Services.EmailTemplateService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService>(provider =>
{
    var jwtSettings = provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
        .GetSection("JwtSettings").Get<ServerEye.Core.Services.JwtSettings>() ?? new ServerEye.Core.Services.JwtSettings();
    var configuration = provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    return new JwtService(jwtSettings, configuration);
});

// Encryption service
var encryptionSettings = configuration.GetSection("Encryption").Get<ServerEye.Core.Configuration.EncryptionSettings>() ?? new ServerEye.Core.Configuration.EncryptionSettings();
builder.Services.AddSingleton(encryptionSettings);
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

builder.Services.AddScoped<IMetricsCacheService, ServerEye.Infrastracture.Caching.MetricsCacheService>();
builder.Services.AddScoped<IServerAccessService, ServerAccessService>();
builder.Services.AddScoped<IServerDiscoveryService, ServerDiscoveryService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IStaticInfoService, StaticInfoService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITicketService, TicketService>();

// OAuth2 services
var oauthSettings = configuration.GetSection("OAuth").Get<ServerEye.Core.Configuration.OAuthSettings>() ?? new ServerEye.Core.Configuration.OAuthSettings();
builder.Services.AddSingleton(oauthSettings);

// Register OAuth providers
builder.Services.AddScoped<ServerEye.Core.Services.OAuth.Providers.GoogleOAuthProvider>();
builder.Services.AddScoped<ServerEye.Core.Services.OAuth.Providers.GitHubOAuthProvider>();
builder.Services.AddScoped<ServerEye.Core.Services.OAuth.Providers.TelegramOAuthProvider>();

// Load servers configuration
var serversConfiguration = configuration.GetSection("ServersConfiguration").Get<ServerEye.Core.Configuration.ServersConfiguration>() ?? new ServerEye.Core.Configuration.ServersConfiguration();
builder.Services.AddSingleton(serversConfiguration);

// Register MockDataProvider
builder.Services.AddScoped<ServerEye.Core.Interfaces.Services.IMockDataProvider, ServerEye.Core.Services.MockDataProvider>();

// Register OAuth provider factory
builder.Services.AddScoped<ServerEye.Core.Services.OAuth.Factory.IOAuthProviderFactory, ServerEye.Core.Services.OAuth.Factory.OAuthProviderFactory>();

builder.Services.AddScoped<IServersService, ServerEye.Core.Services.ServersService>();
builder.Services.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<UserRegisterDtoValidator>();

// Configure Global Exception Handler
builder.Services.AddExceptionHandler<ServerEye.API.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure Health Checks
var serverEyeConnectionString = configuration["DATABASE_CONNECTION_STRING"]
                         ?? configuration.GetConnectionString("ServerEyeDbContext")
                         ?? configuration.GetConnectionString("DefaultConnection")
                         ?? throw new InvalidOperationException("Database connection string not found");

var ticketConnectionString = configuration["TICKET_DB_CONNECTION_STRING"]
                         ?? configuration.GetConnectionString("TicketDbContext")
                         ?? throw new InvalidOperationException("Ticket database connection string not found");

// Log connection strings for debugging
Console.WriteLine($"ServerEye DB Connection: {serverEyeConnectionString}");
Console.WriteLine($"Ticket DB Connection: {ticketConnectionString}");

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: serverEyeConnectionString + ";TrustServerCertificate=true",
        name: "postgres-servereye",
        tags: ["db", "postgres", "ready"])
    .AddNpgSql(
        connectionString: ticketConnectionString + ";TrustServerCertificate=true",
        name: "postgres-tickets",
        tags: ["db", "postgres", "ready"])
    .AddRedis(
        redisConnectionString: configuration["REDIS_CONNECTION_STRING"]
                              ?? redisSettings.ConnectionString,
        name: "redis",
        tags: ["cache", "redis", "ready"]);

// Configure Rate Limiting
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    // Global rate limit - 100 requests per minute per IP
    rateLimiterOptions.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Strict rate limit for authentication endpoints - 5 requests per minute per IP
    rateLimiterOptions.AddPolicy("auth", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    // Moderate rate limit for API endpoints - 30 requests per minute per IP
    rateLimiterOptions.AddPolicy("api", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    rateLimiterOptions.RejectionStatusCode = 429; // Too Many Requests
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Configure JWT Authentication only if not already registered
if (!builder.Services.Any(s => s.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider)))
{
    var publicKeyBase64 = Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY_BASE64") ?? jwtSettings.PublicKeyBase64;
    var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var rsaForValidation = System.Security.Cryptography.RSA.Create();
        rsaForValidation.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsaForValidation),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
    });
}

builder.Services.AddDbContext<ServerEyeDbContext>(
    options =>
    {
        // Priority: Environment variable > Built from individual env vars > ConnectionStrings section
        var connectionString = configuration["DATABASE_CONNECTION_STRING"]
                             ?? BuildConnectionStringFromEnvironment(configuration)
                             ?? configuration.GetConnectionString("ServerEyeDbContext")
                             ?? configuration.GetConnectionString("DefaultConnection");

        startupLogger.LogDebug("ServerEyeDbContext Connection String configured");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Enable connection pooling optimizations
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    });

builder.Services.AddDbContext<TicketDbContext>(
    options =>
    {
        // Priority: Environment variable > ConnectionStrings section
        var connectionString = configuration["TICKET_DB_CONNECTION_STRING"]
                             ?? configuration.GetConnectionString("TicketDbContext");

        startupLogger.LogDebug("TicketDbContext Connection String configured");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Enable connection pooling optimizations
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
// Global Exception Handler (must be first)
app.UseMiddleware<ServerEye.API.Middleware.GlobalExceptionHandlerMiddleware>();

// Response Compression (should be early in pipeline)
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use HTTPS redirection if required
if (securitySettings.RequireHttps && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Use CORS
app.UseCors("AllowFrontend");

// Use Default Files and Static Files for Telegram OAuth callback
app.UseDefaultFiles();
app.UseStaticFiles();

// Use Rate Limiting (must be before Authentication)
app.UseRateLimiter();

// Use Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map Health Check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                tags = e.Value.Tags
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// Liveness probe - simple check that app is running
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness probe - checks if app is ready to serve traffic
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
