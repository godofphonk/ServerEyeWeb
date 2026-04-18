namespace ServerEye.Core.Configuration;

public class CacheSettings
{
    public TimeSpan LiveMetrics { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan HourMetrics { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan DayMetrics { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan MonthMetrics { get; init; } = TimeSpan.FromHours(1);
    public TimeSpan ServerList { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan UserSubscription { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan UserLimits { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan UserProfile { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan StaticInfo { get; init; } = TimeSpan.FromHours(1);
}
