namespace ServerEye.API.Middleware;

using ServerEye.API.Configuration;

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
    private readonly SecuritySettings securitySettings;

    public CspMiddleware(RequestDelegate next, SecuritySettings securitySettings)
    {
        _next = next;
        this.securitySettings = securitySettings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

        string csp;
        if (environment.IsDevelopment() || environment.IsEnvironment("Docker"))
        {
            csp = this.securitySettings.CspDevelopment;
        }
        else
        {
            csp = this.securitySettings.CspProduction;
        }

        context.Response.Headers.ContentSecurityPolicy = csp;
        await _next(context);
    }
}
