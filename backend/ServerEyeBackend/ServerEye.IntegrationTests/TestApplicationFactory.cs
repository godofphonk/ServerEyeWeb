namespace ServerEye.IntegrationTests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Infrastructure;
using Testcontainers.PostgreSql;
using System.Globalization;

public class TestApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithCleanUp(true)
        .Build();
    
    private static readonly System.Security.Cryptography.RSA TestRsa = System.Security.Cryptography.RSA.Create(2048);
    private static readonly string TestPrivateKey = Convert.ToBase64String(TestRsa.ExportPkcs8PrivateKey());
    private static readonly string TestPublicKey = Convert.ToBase64String(TestRsa.ExportSubjectPublicKeyInfo());

    public async Task InitializeAsync()
    {
        await this.postgresContainer.StartAsync();
        
        // Let migrations handle all database creation
        // Don't create any schema here to avoid conflicts
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        // Database is already created in InitializeAsync
        await Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await this.postgresContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        // Use direct DbContext connection to clean data
        var optionsBuilder = new DbContextOptionsBuilder<ServerEyeDbContext>();
        optionsBuilder.UseNpgsql(this.postgresContainer.GetConnectionString());
        
        await using var serverEyeDb = new ServerEyeDbContext(optionsBuilder.Options);
        
        try
        {
            if (await serverEyeDb.Database.CanConnectAsync())
            {
                // Clear all data from tables in correct order (respecting foreign keys)
                await serverEyeDb.Database.ExecuteSqlRawAsync(
                    """
                    DO $$ 
                    BEGIN
                        -- Truncate tables in correct order to avoid foreign key constraints
                        -- Start with dependent tables, then parent tables
                        TRUNCATE TABLE "UserServerAccess" CASCADE;
                        TRUNCATE TABLE "TicketMessages" CASCADE;
                        TRUNCATE TABLE "TicketAttachments" CASCADE;
                        TRUNCATE TABLE "Tickets" CASCADE;
                        TRUNCATE TABLE "Notifications" CASCADE;
                        TRUNCATE TABLE "MonitoredServers" CASCADE;
                        TRUNCATE TABLE "UserExternalLogins" CASCADE;
                        TRUNCATE TABLE "UserSessions" CASCADE;
                        TRUNCATE TABLE "RefreshTokens" CASCADE;
                        TRUNCATE TABLE "PasswordResetTokens" CASCADE;
                        TRUNCATE TABLE "EmailVerifications" CASCADE;
                        TRUNCATE TABLE "AccountDeletions" CASCADE;
                        TRUNCATE TABLE "Users" CASCADE;
                    EXCEPTION
                        WHEN OTHERS THEN
                            -- Ignore errors if tables don't exist yet
                            NULL;
                    END $$;
                    """);
            }
        }
        catch
        {
            // Ignore errors if database doesn't exist yet
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "TestSecretKey123456789012345678901234567890",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:AccessTokenExpiration"] = "01:00:00",
                ["JwtSettings:RefreshTokenExpiration"] = "7.00:00:00",
                ["JwtSettings:PrivateKeyBase64"] = TestPrivateKey,
                ["JwtSettings:PublicKeyBase64"] = TestPublicKey,
                ["JWT_PRIVATE_KEY_BASE64"] = TestPrivateKey,
                ["JWT_PUBLIC_KEY_BASE64"] = TestPublicKey,
                ["ConnectionStrings:DefaultConnection"] = this.postgresContainer.GetConnectionString(),
                ["ConnectionStrings:TicketDbConnection"] = this.postgresContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = "127.0.0.1:6379",
                // Disable email verification for tests
                ["Authentication:RequireEmailVerification"] = "false",
                ["EmailSettings:EnableEmailVerification"] = "false"
            });
        });

        // Override environment variables for JWT keys
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", TestPrivateKey);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_BASE64", TestPublicKey);

        // JWT Authentication is configured in Program.cs using environment variables
        // Override JWT options using PostConfigure to avoid re-registering the scheme
        builder.ConfigureServices(services =>
        {
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
            

            // Remove existing DbContext registrations
            var dbContextDescriptors = services
                .Where(d => 
                    d.ServiceType == typeof(DbContextOptions<ServerEyeDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions<TicketDbContext>) ||
                    d.ServiceType == typeof(ServerEyeDbContext) ||
                    d.ServiceType == typeof(TicketDbContext))
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add PostgreSQL databases using Testcontainers
            services.AddDbContext<ServerEyeDbContext>(options =>
            {
                options.UseNpgsql(this.postgresContainer.GetConnectionString());
            });

            services.AddDbContext<TicketDbContext>(options =>
            {
                options.UseNpgsql(this.postgresContainer.GetConnectionString());
            });

            // Remove Redis and other external dependencies
            var redisDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Redis", StringComparison.OrdinalIgnoreCase) == true ||
                           d.ServiceType.FullName?.Contains("IDistributedCache", StringComparison.Ordinal) == true ||
                           d.ServiceType.FullName?.Contains("IConnectionMultiplexer", StringComparison.Ordinal) == true)
                .ToList();

            foreach (var descriptor in redisDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add in-memory distributed cache instead of Redis
            services.AddDistributedMemoryCache();

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
            
            // Override JwtService with test settings to ensure token generation uses same keys as validation
            var testJwtSettings = new Core.Services.JwtSettings
            {
                SecretKey = "TestSecretKey123456789012345678901234567890",
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
