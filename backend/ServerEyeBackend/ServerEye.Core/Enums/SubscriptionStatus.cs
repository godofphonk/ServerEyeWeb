namespace ServerEye.Core.Enums;

public enum SubscriptionStatus
{
    Active = 0,
    Canceled = 1,
    PastDue = 2,
    Unpaid = 3,
    Trialing = 4,
    Incomplete = 5,
    IncompleteExpired = 6,
    Paused = 7,
    Free = 8
}
