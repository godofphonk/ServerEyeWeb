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
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var configuration = builder.Configuration;

// Configure settings
var jwtSettings = configuration.GetSection("JwtSettings").Get<ServerEye.Core.Services.JwtSettings>() ?? new ServerEye.Core.Services.JwtSettings();
var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>() ?? new SecuritySettings();
var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
var goApiSettings = configuration.GetSection("GoApiSettings").Get<GoApiSettings>() ?? new GoApiSettings();
var cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();
var redisSettings = configuration.GetSection("Redis").Get<RedisSettings>() ?? new RedisSettings();
var emailSettings = configuration.GetSection("EmailSettings").Get<ServerEye.Core.Configuration.EmailSettings>() ?? new ServerEye.Core.Configuration.EmailSettings();

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
    options.Configuration = redisSettings.ConnectionString;
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

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService>(provider =>
{
    var jwtSettings = provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
        .GetSection("JwtSettings").Get<ServerEye.Core.Services.JwtSettings>() ?? new ServerEye.Core.Services.JwtSettings();
    return new JwtService(jwtSettings);
});

// Encryption service
var encryptionSettings = configuration.GetSection("Encryption").Get<ServerEye.Core.Configuration.EncryptionSettings>() ?? new ServerEye.Core.Configuration.EncryptionSettings();
builder.Services.AddSingleton(encryptionSettings);
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

builder.Services.AddScoped<IMetricsCacheService, ServerEye.Infrastracture.Caching.MetricsCacheService>();
builder.Services.AddScoped<IServerAccessService, ServerAccessService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IStaticInfoService, StaticInfoService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITicketService, TicketService>();

builder.Services.AddValidatorsFromAssemblyContaining<UserRegisterDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<ServerEyeDbContext>(
    options =>
        options.UseNpgsql(configuration.GetConnectionString(nameof(ServerEyeDbContext))));

builder.Services.AddDbContext<TicketDbContext>(
    options =>
        options.UseNpgsql(configuration.GetConnectionString(nameof(TicketDbContext))));

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Use Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
