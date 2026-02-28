namespace ServerEye.IntegrationTests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using ServerEye.Infrastracture;
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

    public async Task InitializeAsync()
    {
        await this.postgresContainer.StartAsync();
        
        // Create a client to trigger service initialization
        using var client = this.CreateClient();
        
        // Now create database schema manually
        using var scope = this.Services.CreateScope();
        var serverEyeDb = scope.ServiceProvider.GetRequiredService<ServerEyeDbContext>();
        
        // Create tables manually
        await serverEyeDb.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" uuid PRIMARY KEY,
                ""UserName"" text NOT NULL,
                ""Email"" text NOT NULL,
                ""Password"" text NOT NULL,
                ""Role"" text NOT NULL,
                ""IsEmailVerified"" boolean NOT NULL DEFAULT false,
                ""EmailVerifiedAt"" timestamp with time zone,
                ""PendingEmail"" text,
                ""ServerId"" uuid,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );
            
            CREATE TABLE IF NOT EXISTS ""RefreshTokens"" (
                ""Id"" uuid PRIMARY KEY,
                ""UserId"" uuid NOT NULL,
                ""Token"" text NOT NULL,
                ""ExpiresAt"" timestamp with time zone NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""IsRevoked"" boolean NOT NULL DEFAULT false,
                CONSTRAINT ""FK_RefreshTokens_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
            );
            
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_UserName"" ON ""Users"" (""UserName"");
            CREATE INDEX IF NOT EXISTS ""IX_RefreshTokens_UserId"" ON ""RefreshTokens"" (""UserId"");
        ");
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
        using var scope = this.Services.CreateScope();
        var serverEyeDb = scope.ServiceProvider.GetRequiredService<ServerEyeDbContext>();
        var ticketDb = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        
        // Clear all data from tables if they exist
        try
        {
            if (await serverEyeDb.Database.CanConnectAsync())
            {
                // Check if tables exist before truncating
                var tablesExist = await serverEyeDb.Database.ExecuteSqlRawAsync(
                    @"DO $$ 
                    BEGIN
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'Users') THEN
                            TRUNCATE TABLE ""RefreshTokens"", ""Users"" CASCADE;
                        END IF;
                    END $$;");
            }
        }
        catch
        {
            // Ignore errors if tables don't exist yet
        }
        
        try
        {
            if (await ticketDb.Database.CanConnectAsync())
            {
                // Clear ticket tables if any exist
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Generate RSA keys for JWT in PKCS8 format
        var rsa = System.Security.Cryptography.RSA.Create(2048);
        var privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "TestSecretKey123456789012345678901234567890",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:AccessTokenExpiration"] = "01:00:00",
                ["JwtSettings:RefreshTokenExpiration"] = "7.00:00:00",
                ["JwtSettings:PrivateKeyBase64"] = privateKey,
                ["JwtSettings:PublicKeyBase64"] = publicKey,
                ["JWT_PRIVATE_KEY_BASE64"] = privateKey,
                ["JWT_PUBLIC_KEY_BASE64"] = publicKey,
                ["ConnectionStrings:DefaultConnection"] = this.postgresContainer.GetConnectionString(),
                ["ConnectionStrings:TicketDbConnection"] = this.postgresContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = "localhost:6379"
            });
        });

        // Override environment variables for JWT keys
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", privateKey);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_BASE64", publicKey);

        builder.ConfigureServices(services =>
        {
            // Override JwtSettings with test keys
            var rsa = System.Security.Cryptography.RSA.Create(2048);
            var privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
            var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());

            var testJwtSettings = new ServerEye.Core.Services.JwtSettings
            {
                SecretKey = "TestSecretKey123456789012345678901234567890",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                AccessTokenExpiration = TimeSpan.Parse("01:00:00", CultureInfo.InvariantCulture),
                RefreshTokenExpiration = TimeSpan.Parse("7.00:00:00", CultureInfo.InvariantCulture),
                PrivateKeyBase64 = privateKey,
                PublicKeyBase64 = publicKey
            };

            services.AddSingleton(testJwtSettings);
            
            // Remove existing JwtService and add new one with test settings
            var jwtServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(ServerEye.Core.Interfaces.Services.IJwtService))
                .ToList();
            
            foreach (var descriptor in jwtServiceDescriptors)
            {
                services.Remove(descriptor);
            }
            
            services.AddSingleton<ServerEye.Core.Interfaces.Services.IJwtService>(provider =>
            {
                var settings = provider.GetRequiredService<ServerEye.Core.Services.JwtSettings>();
                return new ServerEye.Core.Services.JwtService(settings, provider.GetRequiredService<IConfiguration>());
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
        });

        builder.UseEnvironment("Testing");
    }
}
