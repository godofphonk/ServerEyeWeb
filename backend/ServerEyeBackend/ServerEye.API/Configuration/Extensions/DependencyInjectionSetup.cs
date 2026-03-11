namespace ServerEye.API.Configuration.Extensions;

using FluentValidation;
using ServerEye.API.Validators;
using ServerEye.Core.Configuration;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;
using ServerEye.Core.Services;
using ServerEye.Infrastructure.Caching;
using ServerEye.Infrastructure.ExternalServices;
using ServerEye.Infrastructure.ExternalServices.GoApi;
using ServerEye.Infrastructure.Repositories;

/// <summary>
/// Dependency injection configuration for services and repositories.
/// </summary>
public static class DependencyInjectionSetup
{
    /// <summary>
    /// Registers all application services and repositories.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration settings
        RegisterSettings(services, configuration);

        // Register repositories
        RegisterRepositories(services);

        // Register core services
        RegisterCoreServices(services);

        // Register infrastructure services
        RegisterInfrastructureServices(services);

        // Register Go API services
        RegisterGoApiServices(services, configuration);

        // Register validators
        services.AddValidatorsFromAssemblyContaining<UserRegisterDtoValidator>();

        return services;
    }

    private static void RegisterSettings(IServiceCollection services, IConfiguration configuration)
    {
        var goApiSettings = configuration.GetSection("GoApiSettings").Get<GoApiSettings>() 
            ?? new GoApiSettings();
        var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>() 
            ?? new EmailSettings();
        var encryptionSettings = configuration.GetSection("Encryption").Get<EncryptionSettings>() 
            ?? new EncryptionSettings();
        var serversConfiguration = configuration.GetSection("ServersConfiguration").Get<ServersConfiguration>() 
            ?? new ServersConfiguration();

        services.AddSingleton(goApiSettings);
        services.AddSingleton(emailSettings);
        services.AddSingleton(encryptionSettings);
        services.AddSingleton(serversConfiguration);
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IServerRepository, ServerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IMonitoredServerRepository, Infrastructure.Repositories.MonitoredServerRepository>();
        services.AddScoped<IUserServerAccessRepository, Infrastructure.Repositories.UserServerAccessRepository>();
        services.AddScoped<ITicketRepository, Infrastructure.Repositories.TicketRepository>();
        services.AddScoped<ITicketMessageRepository, Infrastructure.Repositories.TicketMessageRepository>();
        services.AddScoped<INotificationRepository, Infrastructure.Repositories.NotificationRepository>();
        services.AddScoped<IEmailVerificationRepository, Infrastructure.Repositories.EmailVerificationRepository>();
        services.AddScoped<IPasswordResetTokenRepository, Infrastructure.Repositories.PasswordResetTokenRepository>();
        services.AddScoped<IAccountDeletionRepository, Infrastructure.Repositories.AccountDeletionRepository>();
        services.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddScoped<IEmailTemplateService, ServerEye.API.Services.EmailTemplateService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IMetricsCacheService, Infrastructure.Caching.MetricsCacheService>();
        services.AddScoped<IServerAccessService, ServerAccessService>();
        services.AddScoped<IServerDiscoveryService, ServerDiscoveryService>();
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IStaticInfoService, StaticInfoService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IServersService, ServersService>();
        services.AddScoped<IMockDataProvider, MockDataProvider>();

        // JWT Service with factory
        services.AddScoped<IJwtService>(provider =>
        {
            var jwtSettings = provider.GetRequiredService<IConfiguration>()
                .GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
            var configuration = provider.GetRequiredService<IConfiguration>();
            return new JwtService(jwtSettings, configuration);
        });
    }

    private static void RegisterInfrastructureServices(IServiceCollection services)
    {
        services.AddScoped<IMetricsCacheService, Infrastructure.Caching.MetricsCacheService>();
    }

    private static void RegisterGoApiServices(IServiceCollection services, IConfiguration configuration)
    {
        var goApiSettings = configuration.GetSection("GoApiSettings").Get<GoApiSettings>() 
            ?? new GoApiSettings();

        // Register Go API HttpClient
        services.AddHttpClient<GoApiHttpHandler>(client =>
        {
            client.BaseAddress = goApiSettings.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(goApiSettings.TimeoutSeconds);
        });

        // Register Go API dependencies
        services.AddScoped<GoApiLogger>();
        services.AddScoped<GoApiOperationFactory>();

        // Register GoApiClient with factory pattern
        services.AddScoped<IGoApiClient>(serviceProvider =>
        {
            var httpHandler = serviceProvider.GetRequiredService<GoApiHttpHandler>();
            var logger = serviceProvider.GetRequiredService<GoApiLogger>();
            var operationFactory = new GoApiOperationFactory(httpHandler, logger);
            return new GoApiClient(operationFactory);
        });
    }
}
