namespace ServerEye.Core.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to exceed their subscription plan limits.
/// </summary>
public class PlanLimitExceededException : InvalidOperationException
{
    public PlanLimitExceededException()
        : base()
    {
    }

    public PlanLimitExceededException(string message)
        : base(message)
    {
    }

    public PlanLimitExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public PlanLimitExceededException(
        string limitType,
        int currentValue,
        int maxValue,
        string planName,
        string planType)
        : base($"Plan limit exceeded: {limitType}. Current: {currentValue}, Max: {maxValue}, Plan: {planName}")
    {
        LimitType = limitType;
        CurrentValue = currentValue;
        MaxValue = maxValue;
        PlanName = planName;
        PlanType = planType;
    }

    /// <summary>
    /// The limit type that was exceeded (e.g., "servers", "alerts").
    /// </summary>
    public string LimitType { get; }

    /// <summary>
    /// The current value (e.g., current server count).
    /// </summary>
    public int CurrentValue { get; }

    /// <summary>
    /// The maximum allowed value.
    /// </summary>
    public int MaxValue { get; }

    /// <summary>
    /// The name of the user's current plan.
    /// </summary>
    public string PlanName { get; }

    /// <summary>
    /// The type of the user's current plan.
    /// </summary>
    public string PlanType { get; }
}
