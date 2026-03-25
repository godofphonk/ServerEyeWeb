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

        // Register domain services by logical grouping
        RegisterDomainServices(services);
        
        // Register API-specific services
        RegisterApiServices(services);

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
        var encryptionSettings = new EncryptionSettings 
        { 
            Key = configuration["ENCRYPTION_KEY"] ?? string.Empty 
        };
        var serversConfiguration = configuration.GetSection("ServersConfiguration").Get<ServersConfiguration>() 
            ?? new ServersConfiguration();
        var frontendSettings = configuration.GetSection("FrontendSettings").Get<FrontendSettings>() 
            ?? new FrontendSettings();

        services.AddSingleton(goApiSettings);
        services.AddSingleton(emailSettings);
        services.AddSingleton(encryptionSettings);
        services.AddSingleton(serversConfiguration);
        services.AddSingleton(frontendSettings);
        
        services.Configure<Infrastructure.ExternalServices.Stripe.StripeConfiguration>(
            configuration.GetSection("Stripe"));

        services.Configure<Infrastructure.ExternalServices.YooKassa.YooKassaConfiguration>(
            configuration.GetSection("YooKassa"));
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
        
        services.AddScoped<ServerEye.Core.Interfaces.Repository.Billing.ISubscriptionRepository, Infrastructure.Repositories.Billing.SubscriptionRepository>();
        services.AddScoped<ServerEye.Core.Interfaces.Repository.Billing.IPaymentRepository, Infrastructure.Repositories.Billing.PaymentRepository>();
        services.AddScoped<ServerEye.Core.Interfaces.Repository.Billing.ISubscriptionPlanRepository, Infrastructure.Repositories.Billing.SubscriptionPlanRepository>();
        services.AddScoped<ServerEye.Core.Interfaces.Repository.Billing.IWebhookEventRepository, Infrastructure.Repositories.Billing.WebhookEventRepository>();
    }

    /// <summary>
    /// Registers domain-specific services organized by business logic.
    /// </summary>
    private static void RegisterDomainServices(IServiceCollection services)
    {
        RegisterAuthServices(services);
        RegisterEmailServices(services);
        RegisterEncryptionServices(services);
        RegisterMetricsServices(services);
        RegisterNotificationServices(services);
        RegisterServerServices(services);
        RegisterTicketServices(services);
        RegisterUserServices(services);
        RegisterBillingServices(services);
    }

    /// <summary>
    /// Registers authentication and authorization services.
    /// </summary>
    private static void RegisterAuthServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // JWT Service with factory
        services.AddScoped<IJwtService>(provider =>
        {
            var jwtSettings = provider.GetRequiredService<IConfiguration>()
                .GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
            var configuration = provider.GetRequiredService<IConfiguration>();
            return new JwtService(jwtSettings, configuration);
        });
    }

    /// <summary>
    /// Registers email-related services.
    /// </summary>
    private static void RegisterEmailServices(IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
    }

    /// <summary>
    /// Registers encryption and security services.
    /// </summary>
    private static void RegisterEncryptionServices(IServiceCollection services)
    {
        services.AddScoped<IEncryptionService, EncryptionService>();
    }

    /// <summary>
    /// Registers metrics and monitoring services.
    /// </summary>
    private static void RegisterMetricsServices(IServiceCollection services)
    {
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IMetricsCacheService, Infrastructure.Caching.MetricsCacheService>();
    }

    /// <summary>
    /// Registers notification services.
    /// </summary>
    private static void RegisterNotificationServices(IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
    }

    /// <summary>
    /// Registers server management services.
    /// </summary>
    private static void RegisterServerServices(IServiceCollection services)
    {
        services.AddScoped<IServerAccessService, ServerAccessService>();
        services.AddScoped<IServerDiscoveryService, ServerDiscoveryService>();
        services.AddScoped<IServersService, ServersService>();
        services.AddScoped<IStaticInfoService, StaticInfoService>();
        services.AddScoped<IMockDataProvider, MockDataProvider>();
        services.AddScoped<ISourceManagementService, SourceManagementService>();
    }

    /// <summary>
    /// Registers ticket management services.
    /// </summary>
    private static void RegisterTicketServices(IServiceCollection services)
    {
        services.AddScoped<ITicketService, TicketService>();
    }

    /// <summary>
    /// Registers user management services.
    /// </summary>
    private static void RegisterUserServices(IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
    }

    /// <summary>
    /// Registers API-specific services that don't belong to domain layer.
    /// </summary>
    private static void RegisterApiServices(IServiceCollection services)
    {
        services.AddScoped<IEmailTemplateService, ServerEye.API.Services.EmailTemplateService>();
    }

    /// <summary>
    /// Registers infrastructure services that don't fit into specific domains.
    /// </summary>
    private static void RegisterInfrastructureServices(IServiceCollection services)
    {
        // Infrastructure services are now registered in their respective domain methods
        // This method is kept for future infrastructure-specific services
        // Parameter is kept for consistency with other registration methods
#pragma warning disable IDE0060 // Remove unused parameter
        _ = services; // Suppress unused parameter warning
#pragma warning restore IDE0060
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

    private static void RegisterBillingServices(IServiceCollection services)
    {
        services.AddScoped<ServerEye.Core.Interfaces.Services.Billing.ISubscriptionService, ServerEye.Core.Services.Billing.SubscriptionService>();
        services.AddScoped<ServerEye.Core.Interfaces.Services.Billing.IPaymentService, ServerEye.Core.Services.Billing.PaymentService>();
        services.AddScoped<ServerEye.Core.Interfaces.Services.Billing.IWebhookService, ServerEye.Core.Services.Billing.WebhookService>();
        services.AddScoped<ServerEye.Core.Interfaces.Services.Billing.IPaymentProviderFactory, Infrastructure.Services.Billing.PaymentProviderFactory>();
        services.AddScoped<ServerEye.Core.Interfaces.Services.Billing.IPaymentProvider, Infrastructure.ExternalServices.Stripe.StripePaymentProvider>();
    }
}
