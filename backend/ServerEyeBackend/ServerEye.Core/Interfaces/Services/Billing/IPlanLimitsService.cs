namespace ServerEye.Core.Interfaces.Services.Billing;

using ServerEye.Core.DTOs.Billing;

/// <summary>
/// Service for checking and retrieving subscription plan limits for users.
/// </summary>
public interface IPlanLimitsService
{
    /// <summary>
    /// Gets the plan limits for a user including current usage.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>User plan limits with current usage.</returns>
    public Task<UserPlanLimitsDto> GetUserLimitsAsync(Guid userId);

    /// <summary>
    /// Checks if a user can add another server based on their plan limits.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user can add a server, false otherwise.</returns>
    public Task<bool> CanAddServerAsync(Guid userId);

    /// <summary>
    /// Gets the metrics retention period in days for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Number of days metrics are retained.</returns>
    public Task<int> GetMetricsRetentionDaysAsync(Guid userId);
}
