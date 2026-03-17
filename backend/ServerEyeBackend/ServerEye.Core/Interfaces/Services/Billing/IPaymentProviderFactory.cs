namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.Enums;

public interface IPaymentProviderFactory
{
    public IPaymentProvider GetProvider(PaymentProvider providerType);

    public IPaymentProvider GetDefaultProvider();
}
