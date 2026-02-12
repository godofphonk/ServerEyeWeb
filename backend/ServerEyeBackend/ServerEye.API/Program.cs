using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServerEye.API.Configuration;
using ServerEye.API.Controllers;
using ServerEye.API.Validators;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services;
using ServerEye.Infrastracture;
using ServerEye.Infrastracture.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Configure settings
var jwtSettings = configuration.GetSection("JwtSettings").Get<ServerEye.Core.Services.JwtSettings>() ?? new ServerEye.Core.Services.JwtSettings();
var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>() ?? new SecuritySettings();
var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();

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

builder.Services.AddScoped<IServerRepository, ServerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService>(provider =>
{
    var jwtSettings = provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
        .GetSection("JwtSettings").Get<ServerEye.Core.Services.JwtSettings>() ?? new ServerEye.Core.Services.JwtSettings();
    return new JwtService(jwtSettings);
});

builder.Services.AddValidatorsFromAssemblyContaining<UserRegisterDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<ServerEyeDbContext>(
    options =>
        options.UseNpgsql(configuration.GetConnectionString(nameof(ServerEyeDbContext))));

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
