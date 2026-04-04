namespace ServerEye.UnitTests.Services.Metrics;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Entities;
using ServerEye.Core.Services;

internal class MetricsMapperTests
{
    private static GoApiDataPoint CreateDataPoint(
        double cpuAvg = 50, double cpuMax = 80, double cpuMin = 20,
        double memAvg = 60, double memMax = 90, double memMin = 30,
        double diskAvg = 40, double diskMax = 70,
        double networkAvg = 10, double networkMax = 20,
        double tempAvg = 45, double tempMax = 70,
        double loadAvg = 1.5, double loadMax = 3.0,
        int sampleCount = 10)
    {
        return new GoApiDataPoint
        {
            Timestamp = DateTime.UtcNow,
            CpuAvg = cpuAvg,
            CpuMax = cpuMax,
            CpuMin = cpuMin,
            MemoryAvg = memAvg,
            MemoryMax = memMax,
            MemoryMin = memMin,
            DiskAvg = diskAvg,
            DiskMax = diskMax,
            NetworkAvg = networkAvg,
            NetworkMax = networkMax,
            TempAvg = tempAvg,
            TempMax = tempMax,
            LoadAvg = loadAvg,
            LoadMax = loadMax,
            SampleCount = sampleCount,
        };
    }

    private static Server CreateServer(string hostname = "test-server")
    {
        return new Server
        {
            Id = Guid.NewGuid(),
            ServerId = "srv_test",
            ServerKey = "key_test",
            Hostname = hostname,
            OperatingSystem = "Linux",
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static GoApiMetricsResponse CreateGoApiResponse(
        string serverId = "srv_123",
        string granularity = "5m",
        List<GoApiDataPoint>? dataPoints = null)
    {
        var start = DateTime.UtcNow.AddHours(-1);
        var end = DateTime.UtcNow;

        return new GoApiMetricsResponse
        {
            ServerId = serverId,
            StartTime = start,
            EndTime = end,
            Granularity = granularity,
            DataPoints = dataPoints ?? new List<GoApiDataPoint>(),
            TotalPoints = dataPoints?.Count ?? 0,
        };
    }

    // --- MapDataPoint ---

    [Fact]
    public void MapDataPoint_ShouldMapCpuMetricsCorrectly()
    {
        var goPoint = CreateDataPoint(cpuAvg: 55.5, cpuMax: 90.0, cpuMin: 10.0);

        var result = MetricsMapper.MapDataPoint(goPoint);

        result.Cpu.Avg.Should().Be(55.5);
        result.Cpu.Max.Should().Be(90.0);
        result.Cpu.Min.Should().Be(10.0);
    }

    [Fact]
    public void MapDataPoint_ShouldMapMemoryMetricsCorrectly()
    {
        var goPoint = CreateDataPoint(memAvg: 70.0, memMax: 95.0, memMin: 40.0);

        var result = MetricsMapper.MapDataPoint(goPoint);

        result.Memory.Avg.Should().Be(70.0);
        result.Memory.Max.Should().Be(95.0);
        result.Memory.Min.Should().Be(40.0);
    }

    [Fact]
    public void MapDataPoint_ShouldMapDiskMetricsCorrectly()
    {
        var goPoint = CreateDataPoint(diskAvg: 35.0, diskMax: 60.0);

        var result = MetricsMapper.MapDataPoint(goPoint);

        result.Disk.Avg.Should().Be(35.0);
        result.Disk.Max.Should().Be(60.0);
    }

    [Fact]
    public void MapDataPoint_ShouldMapNetworkMetricsCorrectly()
    {
        var goPoint = CreateDataPoint(networkAvg: 15.0, networkMax: 25.0);

        var result = MetricsMapper.MapDataPoint(goPoint);

        result.Network.Avg.Should().Be(15.0);
        result.Network.Max.Should().Be(25.0);
    }

    [Fact]
    public void MapDataPoint_ShouldMapTemperatureMetricsCorrectly()
    {
        var goPoint = CreateDataPoint(tempAvg: 55.0, tempMax: 80.0);

        var result = MetricsMapper.MapDataPoint(goPoint);

        result.Temperature.Avg.Should().Be(55.0);
        result.Temperature.Max.Should().Be(80.0);
    }

    [Fact]
    public void MapDataPoint_ShouldMapLoadAverageMetricsCorrectly()
    {
        var goPoint = CreateDataPoint(loadAvg: 2.0, loadMax: 4.5);

        var result = MetricsMapper.MapDataPoint(goPoint);

        result.LoadAverage.Avg.Should().Be(2.0);
        result.LoadAverage.Max.Should().Be(4.5);
    }

    [Fact]
    public void MapDataPoint_ShouldMapTimestampCorrectly()
    {
        var expectedTimestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var goPoint = new GoApiDataPoint { Timestamp = expectedTimestamp };

        var result = MetricsMapper.MapDataPoint(goPoint);

        result.Timestamp.Should().Be(expectedTimestamp);
    }

    [Fact]
    public void MapDataPoint_ShouldMapSampleCountCorrectly()
    {
        var goPoint = CreateDataPoint(sampleCount: 42);

        var result = MetricsMapper.MapDataPoint(goPoint);

        result.SampleCount.Should().Be(42);
    }

    // --- CalculateSummary ---

    [Fact]
    public void CalculateSummary_WithEmptyDataPoints_ShouldReturnSummaryWithOnlyTimeRange()
    {
        var start = DateTime.UtcNow.AddHours(-1);
        var end = DateTime.UtcNow;

        var result = MetricsMapper.CalculateSummary(new List<GoApiDataPoint>(), start, end);

        result.Should().NotBeNull();
        result.AvgCpu.Should().Be(0);
        result.TotalDataPoints.Should().Be(0);
        result.TimeRange.Should().BeCloseTo(end - start, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CalculateSummary_WithSingleDataPoint_ShouldReturnCorrectAverages()
    {
        var dataPoint = CreateDataPoint(cpuAvg: 70, cpuMax: 85, cpuMin: 55, memAvg: 60, memMax: 80, memMin: 40, diskAvg: 30, diskMax: 50);
        var start = DateTime.UtcNow.AddHours(-1);
        var end = DateTime.UtcNow;

        var result = MetricsMapper.CalculateSummary(new List<GoApiDataPoint> { dataPoint }, start, end);

        result.AvgCpu.Should().Be(70);
        result.MaxCpu.Should().Be(85);
        result.MinCpu.Should().Be(55);
        result.AvgMemory.Should().Be(60);
        result.MaxMemory.Should().Be(80);
        result.MinMemory.Should().Be(40);
        result.AvgDisk.Should().Be(30);
        result.MaxDisk.Should().Be(50);
        result.TotalDataPoints.Should().Be(1);
    }

    [Fact]
    public void CalculateSummary_WithMultipleDataPoints_ShouldReturnCorrectAverages()
    {
        var dataPoints = new List<GoApiDataPoint>
        {
            CreateDataPoint(cpuAvg: 40, cpuMax: 60, cpuMin: 20, memAvg: 50, memMax: 70, memMin: 30, diskAvg: 20, diskMax: 40),
            CreateDataPoint(cpuAvg: 60, cpuMax: 80, cpuMin: 40, memAvg: 70, memMax: 90, memMin: 50, diskAvg: 40, diskMax: 60),
            CreateDataPoint(cpuAvg: 80, cpuMax: 100, cpuMin: 60, memAvg: 90, memMax: 100, memMin: 70, diskAvg: 60, diskMax: 80),
        };
        var start = DateTime.UtcNow.AddHours(-3);
        var end = DateTime.UtcNow;

        var result = MetricsMapper.CalculateSummary(dataPoints, start, end);

        result.AvgCpu.Should().BeApproximately(60, 0.01);
        result.MaxCpu.Should().Be(100);
        result.MinCpu.Should().Be(20);
        result.AvgMemory.Should().BeApproximately(70, 0.01);
        result.MaxMemory.Should().Be(100);
        result.MinMemory.Should().Be(30);
        result.AvgDisk.Should().BeApproximately(40, 0.01);
        result.MaxDisk.Should().Be(80);
        result.TotalDataPoints.Should().Be(3);
    }

    [Fact]
    public void CalculateSummary_ShouldComputeCorrectTimeRange()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc);
        var dataPoint = CreateDataPoint();

        var result = MetricsMapper.CalculateSummary(new List<GoApiDataPoint> { dataPoint }, start, end);

        result.TimeRange.Should().Be(TimeSpan.FromHours(1));
    }

    // --- MapToResponse ---

    [Fact]
    public void MapToResponse_ShouldMapServerIdCorrectly()
    {
        var goResponse = CreateGoApiResponse(serverId: "srv_abc123");
        var server = CreateServer();

        var result = MetricsMapper.MapToResponse(goResponse, server, false);

        result.ServerId.Should().Be("srv_abc123");
    }

    [Fact]
    public void MapToResponse_ShouldMapServerHostnameAsServerName()
    {
        var goResponse = CreateGoApiResponse();
        var server = CreateServer(hostname: "my-web-server");

        var result = MetricsMapper.MapToResponse(goResponse, server, false);

        result.ServerName.Should().Be("my-web-server");
    }

    [Fact]
    public void MapToResponse_WhenNotCached_ShouldSetIsCachedToFalse()
    {
        var goResponse = CreateGoApiResponse();
        var server = CreateServer();

        var result = MetricsMapper.MapToResponse(goResponse, server, isCached: false);

        result.IsCached.Should().BeFalse();
        result.CachedAt.Should().BeNull();
    }

    [Fact]
    public void MapToResponse_WhenCached_ShouldSetIsCachedToTrueAndPopulateCachedAt()
    {
        var goResponse = CreateGoApiResponse();
        var server = CreateServer();
        var before = DateTime.UtcNow;

        var result = MetricsMapper.MapToResponse(goResponse, server, isCached: true);

        result.IsCached.Should().BeTrue();
        result.CachedAt.Should().NotBeNull();
        result.CachedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MapToResponse_WithDataPoints_ShouldMapAllDataPoints()
    {
        var dataPoints = new List<GoApiDataPoint>
        {
            CreateDataPoint(cpuAvg: 30),
            CreateDataPoint(cpuAvg: 50),
            CreateDataPoint(cpuAvg: 70),
        };
        var goResponse = CreateGoApiResponse(dataPoints: dataPoints);
        var server = CreateServer();

        var result = MetricsMapper.MapToResponse(goResponse, server, false);

        result.DataPoints.Should().HaveCount(3);
        result.DataPoints[0].Cpu.Avg.Should().Be(30);
        result.DataPoints[1].Cpu.Avg.Should().Be(50);
        result.DataPoints[2].Cpu.Avg.Should().Be(70);
    }

    [Fact]
    public void MapToResponse_WithNullDataPoints_ShouldReturnEmptyDataPointsList()
    {
        var goResponse = new GoApiMetricsResponse
        {
            ServerId = "srv_test",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            Granularity = "5m",
            DataPoints = null!,
            TotalPoints = 0,
        };
        var server = CreateServer();

        var result = MetricsMapper.MapToResponse(goResponse, server, false);

        result.DataPoints.Should().NotBeNull();
        result.DataPoints.Should().BeEmpty();
    }

    [Fact]
    public void MapToResponse_ShouldMapGranularityCorrectly()
    {
        var goResponse = CreateGoApiResponse(granularity: "1h");
        var server = CreateServer();

        var result = MetricsMapper.MapToResponse(goResponse, server, false);

        result.Granularity.Should().Be("1h");
    }

    [Fact]
    public void MapToResponse_ShouldMapTimeRangeCorrectly()
    {
        var expectedStart = new DateTime(2024, 6, 15, 8, 0, 0, DateTimeKind.Utc);
        var expectedEnd = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var goResponse = new GoApiMetricsResponse
        {
            ServerId = "srv_test",
            StartTime = expectedStart,
            EndTime = expectedEnd,
            Granularity = "5m",
            DataPoints = new List<GoApiDataPoint>(),
            TotalPoints = 0,
        };
        var server = CreateServer();

        var result = MetricsMapper.MapToResponse(goResponse, server, false);

        result.StartTime.Should().Be(expectedStart);
        result.EndTime.Should().Be(expectedEnd);
    }

    [Fact]
    public void MapToResponse_ShouldIncludeNonNullSummary()
    {
        var goResponse = CreateGoApiResponse(dataPoints: new List<GoApiDataPoint> { CreateDataPoint() });
        var server = CreateServer();

        var result = MetricsMapper.MapToResponse(goResponse, server, false);

        result.Summary.Should().NotBeNull();
    }

    [Fact]
    public void MapToResponse_ShouldPreserveOptionalMessage()
    {
        var goResponse = new GoApiMetricsResponse
        {
            ServerId = "srv_test",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            Granularity = "5m",
            DataPoints = new List<GoApiDataPoint>(),
            TotalPoints = 0,
            Message = "No data available for the selected time range",
        };
        var server = CreateServer();

        var result = MetricsMapper.MapToResponse(goResponse, server, false);

        result.Message.Should().Be("No data available for the selected time range");
    }
}
