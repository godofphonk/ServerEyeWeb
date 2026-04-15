namespace ServerEye.API.Configuration.Extensions;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using ServerEye.Core.Configuration;

/// <summary>
/// Middleware configuration setup.
/// </summary>
public static class MiddlewareSetup
{
    /// <summary>
    /// Adds CORS, response compression, and rate limiting.
    /// </summary>
    public static IServiceCollection AddMiddlewareConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>()
            ?? new CorsSettings();
        var securitySettings = configuration.GetSection("Security").Get<ServerEye.API.Configuration.SecuritySettings>()
            ?? new ServerEye.API.Configuration.SecuritySettings();

        // Configure Response Compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                ["application/json", "application/xml", "text/plain", "text/json"]);
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Optimal;
        });

        // Configure CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins(corsSettings.AllowedOrigins)
                      .WithMethods(corsSettings.AllowedMethods)
                      .WithHeaders(corsSettings.AllowedHeaders)
                      .AllowCredentials());
        });

        // Configure Rate Limiting
        if (securitySettings.EnableRateLimiting)
        {
            services.AddRateLimiter(rateLimiterOptions =>
            {
                // Global rate limit - configurable per environment
                rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = securitySettings.GlobalRateLimitPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10
                        }));

                // Strict rate limit for authentication endpoints
                rateLimiterOptions.AddPolicy("auth", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = securitySettings.AuthRateLimitPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 2
                        }));
            });
        }

        // Configure Global Exception Handler
        services.AddExceptionHandler<Middleware.GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    /// Configures middleware pipeline.
    /// </summary>
    public static IApplicationBuilder UseMiddlewareConfiguration(this IApplicationBuilder app)
    {
        app.UseResponseCompression();
        app.UseCors("AllowFrontend");

        // Only use RateLimiter if it was registered
        var securitySettings = app.ApplicationServices.GetService<ServerEye.API.Configuration.SecuritySettings>();
        if (securitySettings?.EnableRateLimiting == true)
        {
            app.UseRateLimiter();
        }

        app.UseExceptionHandler();

        return app;
    }
}
