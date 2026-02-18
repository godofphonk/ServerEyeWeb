namespace ServerEye.Core.Configuration;

public class CacheSettings
{
    public TimeSpan LiveMetrics { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan HourMetrics { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan DayMetrics { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan MonthMetrics { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ServerList { get; set; } = TimeSpan.FromMinutes(10);
}
