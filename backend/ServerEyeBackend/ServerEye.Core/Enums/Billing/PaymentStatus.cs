namespace ServerEye.Core.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
    Canceled = 5,
    Refunded = 6,
    PartiallyRefunded = 7
}
