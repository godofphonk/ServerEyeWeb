namespace ServerEye.IntegrationTests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using ServerEye.Infrastracture;
using Testcontainers.PostgreSql;

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
        
        // Create database schema
        using var scope = this.Services.CreateScope();
        var serverEyeDb = scope.ServiceProvider.GetRequiredService<ServerEyeDbContext>();
        var ticketDb = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        
        await serverEyeDb.Database.EnsureCreatedAsync();
        await ticketDb.Database.EnsureCreatedAsync();
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
        
        await serverEyeDb.Database.EnsureDeletedAsync();
        await serverEyeDb.Database.EnsureCreatedAsync();
        
        await ticketDb.Database.EnsureDeletedAsync();
        await ticketDb.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Generate RSA keys for JWT
        var rsa = System.Security.Cryptography.RSA.Create(2048);
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());

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
                ["ConnectionStrings:DefaultConnection"] = this.postgresContainer.GetConnectionString(),
                ["ConnectionStrings:TicketDbConnection"] = this.postgresContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = "localhost:6379"
            });
        });

        builder.ConfigureServices(services =>
        {
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
