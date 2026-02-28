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
    
    private static readonly System.Security.Cryptography.RSA TestRsa = System.Security.Cryptography.RSA.Create(2048);
    private static readonly string TestPrivateKey = Convert.ToBase64String(TestRsa.ExportPkcs8PrivateKey());
    private static readonly string TestPublicKey = Convert.ToBase64String(TestRsa.ExportSubjectPublicKeyInfo());

    public async Task InitializeAsync()
    {
        await this.postgresContainer.StartAsync();
        
        // Create database schema directly via DbContext
        var optionsBuilder = new DbContextOptionsBuilder<ServerEyeDbContext>();
        optionsBuilder.UseNpgsql(this.postgresContainer.GetConnectionString());
        
        using var serverEyeDb = new ServerEyeDbContext(optionsBuilder.Options);
        await serverEyeDb.Database.EnsureCreatedAsync();
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
        // Use direct DbContext connection instead of Services
        var optionsBuilder = new DbContextOptionsBuilder<ServerEyeDbContext>();
        optionsBuilder.UseNpgsql(this.postgresContainer.GetConnectionString());
        
        using var serverEyeDb = new ServerEyeDbContext(optionsBuilder.Options);
        
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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
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
                ["ConnectionStrings:Redis"] = "localhost:6379"
            });
        });

        // Override environment variables for JWT keys
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", TestPrivateKey);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_BASE64", TestPublicKey);

        // Configure JWT Authentication for tests
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "TestIssuer",
                    ValidateAudience = true,
                    ValidAudience = "TestAudience",
                    ValidateLifetime = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(TestRsa.ExportParameters(false)),
                    ClockSkew = TimeSpan.Zero
                };
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
