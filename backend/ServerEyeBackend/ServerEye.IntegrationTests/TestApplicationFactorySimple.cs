namespace ServerEye.IntegrationTests;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using DotNet.Testcontainers.Images;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ServerEye.API;
using ServerEye.Infrastructure;
using ServerEye.Infrastructure.Data;
using StackExchange.Redis;

public class TestApplicationFactorySimple : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly System.Security.Cryptography.RSA TestRsa = System.Security.Cryptography.RSA.Create(2048);
    private static readonly string TestPrivateKey = Convert.ToBase64String(TestRsa.ExportPkcs8PrivateKey());
    private static readonly string TestPublicKey = Convert.ToBase64String(TestRsa.ExportSubjectPublicKeyInfo());

    // In-memory collection for OpenTelemetry spans
    private readonly List<Activity> _exportedActivities = new();
    public IEnumerable<Activity> ExportedActivities => _exportedActivities;

    public async Task InitializeAsync()
    {
        // No Testcontainers - use in-memory database
        await Task.CompletedTask;
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        // Database is created in-memory
        await Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task ResetDatabaseAsync()
    {
        // In-memory database is automatically reset
        await Task.CompletedTask;
    }

    protected override IHostBuilder? CreateHostBuilder()
    {
        return base.CreateHostBuilder();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "ThisIsASecretKeyForDevelopment123456789",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:AccessTokenExpiration"] = "01:00:00",
                ["JwtSettings:RefreshTokenExpiration"] = "7.00:00:00",
                ["JwtSettings:PrivateKeyBase64"] = TestPrivateKey,
                ["JwtSettings:PublicKeyBase64"] = TestPublicKey,
                ["JWT_PRIVATE_KEY_BASE64"] = TestPrivateKey,
                ["JWT_PUBLIC_KEY_BASE64"] = TestPublicKey,
                // Use in-memory database
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["ConnectionStrings:ServerEyeDbContext"] = "DataSource=:memory:",
                ["ConnectionStrings:TicketDbContext"] = "DataSource=:memory:",
                ["ConnectionStrings:BillingDbContext"] = "DataSource=:memory:",
                ["ConnectionStrings:Redis"] = "127.0.0.1:6379",
                // Disable email verification for tests
                ["Authentication:RequireEmailVerification"] = "false",
                ["EmailSettings:EnableEmailVerification"] = "false",
                // Enable OAuth providers for tests with real configuration
                ["OAuth:Google:Enabled"] = "true",
                ["OAuth:Google:ClientId"] = "191261163109-57ph58fcvbf97p4pc4b9vq0e34o7hbi3.apps.googleusercontent.com",
                ["OAuth:Google:ClientSecret"] = "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",
                ["OAuth:Google:RedirectUri"] = "http://127.0.0.1:5246/api/auth/oauth/callback",
                ["OAuth:GitHub:Enabled"] = "true",
                ["OAuth:GitHub:ClientId"] = "Ov23liahEWZhQvi65PZH",
                ["OAuth:GitHub:ClientSecret"] = "f61b09ccef2fdada3b6b5a66e7a60d26a40fef8e",
                ["OAuth:GitHub:RedirectUri"] = "http://127.0.0.1:5246/api/auth/oauth/callback",
                ["OAuth:Telegram:Enabled"] = "true",
                ["OAuth:Telegram:BotId"] = "8364624365",
                ["OAuth:Telegram:BotToken"] = "8364624365:AAH0MDDknDt9MOAjMvSGTDIzUa7jYFN8AtU",
                ["OAuth:Telegram:RedirectUri"] = "http://127.0.0.1:3000/oauth/callback",
                // Disable Redis instrumentation for tests
                ["OpenTelemetry:DisableRedisInstrumentation"] = "true",
                // Disable database initialization for tests
                ["DatabaseInitialization:Enabled"] = "false"
            });
        });

        // Override environment variables for JWT keys
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", TestPrivateKey);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_BASE64", TestPublicKey);

        builder.UseEnvironment("Testing");

        // Override logging configuration to remove OpenTelemetry logging
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL existing OpenTelemetry configuration completely
            var openTelemetryDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("OpenTelemetry", StringComparison.Ordinal) == true ||
                           d.ServiceType.FullName?.Contains("TracerProvider", StringComparison.Ordinal) == true ||
                           d.ServiceType.FullName?.Contains("LoggerProvider", StringComparison.Ordinal) == true)
                .ToList();

            foreach (var descriptor in openTelemetryDescriptors)
            {
                services.Remove(descriptor);
            }

            // Remove ALL Entity Framework Core related services
            var efCoreDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true ||
                           d.ServiceType.FullName?.Contains("DbContext", StringComparison.Ordinal) == true ||
                           d.ServiceType.FullName?.Contains("IDbContext", StringComparison.Ordinal) == true ||
                           d.ServiceType.FullName?.Contains("Database", StringComparison.Ordinal) == true)
                .ToList();

            foreach (var descriptor in efCoreDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add In-Memory Exporter for OpenTelemetry traces (without Redis instrumentation)
            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddInMemoryExporter(_exportedActivities);
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddPrometheusExporter();
                });

            // PostConfigure JWT options to use test keys
            var publicKeyBytes = Convert.FromBase64String(TestPublicKey);
            services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    var rsaForValidation = System.Security.Cryptography.RSA.Create();
                    rsaForValidation.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                    options.TokenValidationParameters.IssuerSigningKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsaForValidation);
                    options.TokenValidationParameters.ValidIssuer = "TestIssuer";
                    options.TokenValidationParameters.ValidAudience = "TestAudience";
                });

            // Remove existing DbContext registrations - this is now redundant but kept for safety
            var dbContextDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ServerEye.Infrastructure.ServerEyeDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions<ServerEye.Infrastructure.TicketDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions<ServerEye.Infrastructure.Data.BillingDbContext>) ||
                    d.ServiceType == typeof(ServerEye.Infrastructure.ServerEyeDbContext) ||
                    d.ServiceType == typeof(ServerEye.Infrastructure.TicketDbContext) ||
                    d.ServiceType == typeof(ServerEye.Infrastructure.Data.BillingDbContext))
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add in-memory databases
            services.AddDbContext<ServerEye.Infrastructure.ServerEyeDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestServerEyeDb");
            });

            services.AddDbContext<ServerEye.Infrastructure.TicketDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestTicketDb");
            });

            services.AddDbContext<ServerEye.Infrastructure.Data.BillingDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestBillingDb");
            });

            // Remove all Redis-related services including IConnectionMultiplexer
            var redisDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Redis", StringComparison.OrdinalIgnoreCase) == true ||
                           d.ServiceType.FullName?.Contains("IDistributedCache", StringComparison.Ordinal) == true ||
                           d.ServiceType == typeof(IConnectionMultiplexer))
                .ToList();

            foreach (var descriptor in redisDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add mock IConnectionMultiplexer to satisfy Redis dependencies
            var mockConnection = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            mockConnection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
            mockConnection.Setup(c => c.GetEndPoints(It.IsAny<bool>())).Returns(Array.Empty<EndPoint>());
            mockConnection.Setup(c => c.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>())).Returns(Mock.Of<IServer>());
            mockConnection.Setup(c => c.Configuration).Returns("localhost:6379");
            mockConnection.Setup(c => c.IsConnected).Returns(true);

            services.AddSingleton<IConnectionMultiplexer>(mockConnection.Object);

            // Add in-memory distributed cache instead of Redis
            services.AddDistributedMemoryCache();

            // Remove real payment providers and add mocks
            var paymentProviderDescriptors = services
                .Where(d => d.ServiceType == typeof(Core.Interfaces.Services.Billing.IPaymentProviderFactory) ||
                           d.ServiceType == typeof(Core.Interfaces.Services.Billing.IPaymentProvider))
                .ToList();

            foreach (var descriptor in paymentProviderDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add mock payment providers
            var mockStripeProvider = new Mock<Core.Interfaces.Services.Billing.IPaymentProvider>();
            mockStripeProvider.Setup(p => p.ProviderType).Returns(Core.Enums.PaymentProvider.Stripe);
            mockStripeProvider.Setup(p => p.VerifyWebhookSignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            mockStripeProvider.Setup(p => p.CreatePaymentIntentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Core.DTOs.Billing.CreatePaymentIntentResponse
                {
                    PaymentIntentId = "pi_test_123456",
                    ClientSecret = "pi_test_123456_secret_test"
                });
            mockStripeProvider.Setup(p => p.ParseWebhookEventAsync(It.IsAny<string>()))
                .ReturnsAsync(("payment_intent.succeeded", new { id = "evt_test" }));

            var mockYooKassaProvider = new Mock<Core.Interfaces.Services.Billing.IPaymentProvider>();
            mockYooKassaProvider.Setup(p => p.ProviderType).Returns(Core.Enums.PaymentProvider.YooKassa);
            mockYooKassaProvider.Setup(p => p.VerifyWebhookSignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            mockYooKassaProvider.Setup(p => p.CreatePaymentIntentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Core.DTOs.Billing.CreatePaymentIntentResponse
                {
                    PaymentIntentId = "yk_test_123456",
                    ClientSecret = "yk_test_123456_secret_test"
                });
            mockYooKassaProvider.Setup(p => p.ParseWebhookEventAsync(It.IsAny<string>()))
                .ReturnsAsync(("payment.succeeded", new { id = "evt_test" }));

            var mockProviderFactory = new Mock<Core.Interfaces.Services.Billing.IPaymentProviderFactory>();
            mockProviderFactory.Setup(f => f.GetProvider(Core.Enums.PaymentProvider.Stripe))
                .Returns(mockStripeProvider.Object);
            mockProviderFactory.Setup(f => f.GetProvider(Core.Enums.PaymentProvider.YooKassa))
                .Returns(mockYooKassaProvider.Object);

            services.AddSingleton(mockProviderFactory.Object);

            // Remove all existing health check registrations
            var healthCheckServiceDescriptors = services
                .Where(descriptor => descriptor.ServiceType.FullName?.Contains("HealthCheck", StringComparison.Ordinal) == true)
                .ToList();

            foreach (var descriptor in healthCheckServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add simple test health checks that always return healthy
            services.AddHealthChecks()
                .AddCheck("test-health", () => HealthCheckResult.Healthy("Test environment"))
                .AddCheck("postgres-servereye", () => HealthCheckResult.Healthy("Test DB"))
                .AddCheck("postgres-tickets", () => HealthCheckResult.Healthy("Test Tickets DB"))
                .AddCheck("redis", () => HealthCheckResult.Healthy("Test Redis"));

            // Override OAuthSettings with test configuration
            var testOAuthSettings = new Core.Configuration.OAuthSettings
            {
                Google = new Core.Configuration.GoogleSettings
                {
                    Enabled = true,
                    ClientId = "191261163109-57ph58fcvbf97p4pc4b9vq0e34o7hbi3.apps.googleusercontent.com",
                    ClientSecret = "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",
                    RedirectUri = new Uri("http://127.0.0.1:5246/api/auth/oauth/callback")
                },
                GitHub = new Core.Configuration.GitHubSettings
                {
                    Enabled = true,
                    ClientId = "Ov23liahEWZhQvi65PZH",
                    ClientSecret = "f61b09ccef2fdada3b6b5a66e7a60d26a40fef8e",
                    RedirectUri = new Uri("http://127.0.0.1:5246/api/auth/oauth/callback")
                },
                Telegram = new Core.Configuration.TelegramSettings
                {
                    Enabled = true,
                    BotId = "8364624365",
                    BotToken = "8364624365:AAH0MDDknDt9MOAjMvSGTDIzUa7jYFN8AtU",
                    RedirectUri = new Uri("http://127.0.0.1:3000/oauth/callback")
                }
            };

            // Remove existing OAuthSettings
            var oauthSettingsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Core.Configuration.OAuthSettings));
            if (oauthSettingsDescriptor != null)
            {
                services.Remove(oauthSettingsDescriptor);
            }

            // Add test OAuthSettings
            services.AddSingleton(testOAuthSettings);

            // Override JwtService with test settings to ensure token generation uses same keys as validation
            var testJwtSettings = new Core.Services.JwtSettings
            {
                SecretKey = "ThisIsASecretKeyForDevelopment123456789",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                AccessTokenExpiration = TimeSpan.Parse("01:00:00", CultureInfo.InvariantCulture),
                RefreshTokenExpiration = TimeSpan.Parse("7.00:00:00", CultureInfo.InvariantCulture),
                PrivateKeyBase64 = TestPrivateKey,
                PublicKeyBase64 = TestPublicKey
            };

            // Remove existing JwtSettings and JwtService
            var jwtSettingsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Core.Services.JwtSettings));
            if (jwtSettingsDescriptor != null)
            {
                services.Remove(jwtSettingsDescriptor);
            }

            var jwtServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Core.Interfaces.Services.IJwtService));
            if (jwtServiceDescriptor != null)
            {
                services.Remove(jwtServiceDescriptor);
            }

            // Add test JwtSettings and JwtService
            services.AddSingleton(testJwtSettings);
            services.AddSingleton<Core.Interfaces.Services.IJwtService>(provider =>
            {
                var settings = provider.GetRequiredService<Core.Services.JwtSettings>();
                return new Core.Services.JwtService(settings, provider.GetRequiredService<IConfiguration>());
            });
        });

        builder.UseEnvironment("Testing");
    }
}
