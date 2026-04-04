namespace ServerEye.API.Middleware;

/// <summary>
/// Extension method for registering CSP middleware.
/// </summary>
public static class CspMiddlewareExtensions
{
    public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CspMiddleware>();
    }
}

/// <summary>
/// Content Security Policy middleware to allow localhost connections during development.
/// </summary>
public class CspMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public CspMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";

        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
            environment.Equals("Docker", StringComparison.OrdinalIgnoreCase))
        {
            // Allow localhost connections in development
            var csp = "default-src 'self' 'unsafe-inline' 'unsafe-eval' " +
                     "connect-src 'self' ws: wss: http://localhost:* https://localhost:* http://127.0.0.1:* https://127.0.0.1:* " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval' " +
                     "style-src 'self' 'unsafe-inline' " +
                     "img-src 'self' data: blob: " +
                     "font-src 'self' data:";

            context.Response.Headers.ContentSecurityPolicy = csp;
        }
        else
        {
            // Stricter CSP for production
            var csp = "default-src 'self'; " +
                     "connect-src 'self' https:; " +
                     "script-src 'self' 'unsafe-inline'; " +
                     "style-src 'self' 'unsafe-inline'; " +
                     "img-src 'self' data: blob:; " +
                     "font-src 'self' data:;";

            context.Response.Headers.ContentSecurityPolicy = csp;
        }

        await _next(context);
    }
}
