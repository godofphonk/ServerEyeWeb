using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ServerEye.Core.Services.OAuth;
using ServerEye.Core.Services.Database;

namespace ServerEye.API.Configuration.Extensions;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddOpenTelemetryConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var disableAllInstrumentation = configuration.GetValue<bool>("OpenTelemetry:DisableAllInstrumentation", false);
        var isTesting = configuration.GetValue<bool>("Testing", false);
        
        if (disableAllInstrumentation || isTesting)
        {
            return services;
        }

        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "servereye-backend";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
        var enableRedisInstrumentation = !configuration.GetValue<bool>("OpenTelemetry:DisableRedisInstrumentation", false);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path;
                            
                            // Exclude health and OAuth endpoints from tracing
                            return !path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) &&
                                   !path.StartsWithSegments("/api/auth/oauth", StringComparison.OrdinalIgnoreCase);
                        };
                        options.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            activity.SetTag("http.request.path", httpRequest.Path);
                            activity.SetTag("http.request.query", httpRequest.QueryString.ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            activity.SetTag("http.response.status_code", httpResponse.StatusCode);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                        {
                            activity.SetTag("http.request.method", httpRequestMessage.Method.ToString());
                            activity.SetTag("http.request.url", httpRequestMessage.RequestUri?.ToString());
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.command.text", command.CommandText);
                            activity.SetTag("db.command.type", command.CommandType.ToString());
                        };
                    })
                    .AddSource(serviceName)
                    .AddSource("ServerEye.OAuth") // Add OAuth activity source
                    .SetSampler(new AlwaysOnSampler())
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });

                if (enableRedisInstrumentation)
                {
                    tracing.AddRedisInstrumentation(connection =>
                    {
                        connection.SetVerboseDatabaseStatements = true;
                        connection.EnrichActivityWithTimingEvents = true;
                    });
                }
            })
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(serviceName)
                .AddMeter("ServerEye.OAuth") // Add OAuth metrics
                .AddMeter("ServerEye.PostgreSQL") // Add PostgreSQL metrics
                .AddPrometheusExporter()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                }));

        // Register OAuthMetrics as singleton
        services.AddSingleton<OAuthMetrics>();
        
        // Register PostgreSQL metrics
        services.AddSingleton<PostgreSQLMetrics>();
        services.Configure<PostgreSQLMonitoringOptions>(configuration.GetSection("PostgreSQLMonitoring"));

        // Configure logging with OpenTelemetry
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName, serviceVersion: serviceVersion)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                        ["host.name"] = Environment.MachineName
                    }));

                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;

                options.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri(otlpEndpoint);
                    otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                });
            });
        });

        return services;
    }
}
