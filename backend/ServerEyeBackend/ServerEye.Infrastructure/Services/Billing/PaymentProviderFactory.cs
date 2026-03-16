namespace ServerEye.Infrastructure.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Services.Billing;

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IEnumerable<IPaymentProvider> providers;
    private readonly ILogger<PaymentProviderFactory> logger;

    public PaymentProviderFactory(
        IEnumerable<IPaymentProvider> providers,
        ILogger<PaymentProviderFactory> logger)
    {
        this.providers = providers;
        this.logger = logger;
    }

    public IPaymentProvider GetProvider(PaymentProvider providerType)
    {
        var provider = providers.FirstOrDefault(p => p.ProviderType == providerType);
        
        if (provider == null)
        {
            logger.LogError("Payment provider {ProviderType} not found", providerType);
            throw new InvalidOperationException($"Payment provider {providerType} is not configured");
        }

        return provider;
    }

    public IPaymentProvider GetDefaultProvider()
    {
        return GetProvider(PaymentProvider.Stripe);
    }
}
