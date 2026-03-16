namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.Enums;

public interface IPaymentProviderFactory
{
    IPaymentProvider GetProvider(PaymentProvider providerType);
    
    IPaymentProvider GetDefaultProvider();
}
