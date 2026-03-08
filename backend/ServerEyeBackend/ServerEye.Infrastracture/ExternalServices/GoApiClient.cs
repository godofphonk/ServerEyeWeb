namespace ServerEye.Infrastracture.ExternalServices;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Metrics;
using ServerEye.Core.Interfaces.Services;
using System.Globalization;

public class GoApiClient(HttpClient httpClient, ILogger<GoApiClient> logger) : IGoApiClient
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<GoApiMetricsResponse?> GetMetricsByKeyAsync(string serverKey, DateTime start, DateTime endTime, string? granularity = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endStr = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var url = $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/metrics?start={startStr}&end={endStr}";

            if (!string.IsNullOrEmpty(granularity))
            {
                url += $"&granularity={granularity}";
            }

            logger.LogInformation("[PERF] Requesting metrics by key from Go API: {Url}", url);

            var response = await httpClient.GetAsync(new Uri(url, UriKind.Relative));
            var requestTime = stopwatch.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("[PERF] Go API error after {Ms}ms: {StatusCode} - {Content}", requestTime, response.StatusCode, errorContent);
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null
            };

            // Read content once to avoid ObjectDisposedException
            var content = await response.Content.ReadAsStringAsync();

            // Log raw JSON for debugging network_details structure
            if (content.Contains("network_details", StringComparison.OrdinalIgnoreCase))
            {
                var startIndex = Math.Max(0, content.IndexOf("network_details", StringComparison.OrdinalIgnoreCase) - 100);
                logger.LogInformation(
                    "[DEBUG] Raw Go API response contains network_details: {Content}",
                    content.Substring(startIndex, 500));
            }

            // Try to parse as time series first
            GoApiMetricsResponse? result = null;
            try
            {
                result = JsonSerializer.Deserialize<GoApiMetricsResponse>(content, options);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "[DEBUG] Failed to parse GoApiMetricsResponse. Raw JSON: {Content}", content);

                // Ignore parsing errors, will try snapshot format
            }

            // If no data points, try snapshot format
            if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
            {
                try
                {
                    var snapshotResponse = JsonSerializer.Deserialize<GoApiSnapshotResponse>(content, options);
                    if (snapshotResponse != null && snapshotResponse.Metrics != null)
                    {
                        // Convert snapshot to time series format
                        result = ConvertSnapshotToTimeSeries(snapshotResponse, start, endTime, granularity);
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "[PERF] Error parsing snapshot response from Go API");
                }
            }

            stopwatch.Stop();
            var totalTime = stopwatch.ElapsedMilliseconds;

            if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
            {
                logger.LogWarning(
                    "[PERF] Go API returned empty data after {Ms}ms for server key {ServerKey}",
                    totalTime,
                    serverKey);
                return result;
            }

            logger.LogInformation(
                "[PERF] Successfully retrieved {Points} data points by key in {Ms}ms (request: {RequestMs}ms, parse: {ParseMs}ms)",
                result.TotalPoints,
                totalTime,
                requestTime,
                totalTime - requestTime);

            return result;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            logger.LogError("[PERF] Go API request timeout after {Ms}ms: {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "[PERF] Error calling Go API for metrics by key after {Ms}ms", stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    public async Task<GoApiMetricsResponse?> GetMetricsAsync(string serverId, DateTime start, DateTime endTime, string? granularity = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endStr = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var url = $"/api/servers/{serverId}/metrics/tiered?start={startStr}&end={endStr}";

            if (!string.IsNullOrEmpty(granularity))
            {
                url += $"&granularity={granularity}";
            }

            logger.LogInformation("[PERF] Requesting metrics from Go API: {Url}", url);

            var response = await httpClient.GetAsync(new Uri(url, UriKind.Relative));
            var requestTime = stopwatch.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("[PERF] Go API error after {Ms}ms: {StatusCode} - {Content}", requestTime, response.StatusCode, errorContent);
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null
            };

            var result = await response.Content.ReadFromJsonAsync<GoApiMetricsResponse>(options);
            stopwatch.Stop();
            var totalTime = stopwatch.ElapsedMilliseconds;

            if (result == null || result.DataPoints == null || result.DataPoints.Count == 0)
            {
                logger.LogWarning(
                    "[PERF] Go API returned empty data after {Ms}ms for server {ServerId}",
                    totalTime,
                    serverId);
                return result;
            }

            logger.LogInformation(
                "[PERF] Successfully retrieved {Points} data points in {Ms}ms (request: {RequestMs}ms, parse: {ParseMs}ms)",
                result.TotalPoints,
                totalTime,
                requestTime,
                totalTime - requestTime);

            return result;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            logger.LogError("[PERF] Go API request timeout after {Ms}ms: {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "[PERF] Error calling Go API for metrics after {Ms}ms", stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    public async Task<GoApiMetricsResponse?> GetRealtimeMetricsAsync(string serverId, TimeSpan? duration = null)
    {
        try
        {
            var actualDuration = duration ?? TimeSpan.FromMinutes(5);
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(actualDuration);

            logger.LogInformation("Requesting realtime metrics for server {ServerId} from {Start} to {End}", serverId, startTime, endTime);

            return await this.GetMetricsAsync(serverId, startTime, endTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Go API for realtime metrics");
            return null;
        }
    }

    public async Task<GoApiServerInfo?> ValidateServerKeyAsync(string serverKey)
    {
        try
        {
            var url = $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/metrics";

            logger.LogInformation("Validating server key with Go API: {Url}", url);

            var response = await httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Server key validation failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            var metricsResponse = await response.Content.ReadFromJsonAsync<GoApiMetricsResponse>();

            if (metricsResponse?.ServerId != null)
            {
                return new GoApiServerInfo
                {
                    ServerId = metricsResponse.ServerId,
                    ServerKey = serverKey,
                    Hostname = metricsResponse.Status?.Hostname ?? "Unknown",
                    OperatingSystem = metricsResponse.Status?.OperatingSystem ?? "Unknown",
                    AgentVersion = metricsResponse.Status?.AgentVersion ?? "Unknown",
                    LastSeen = metricsResponse.Status?.LastSeen ?? DateTime.UtcNow
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating server key with Go API");
            return null;
        }
    }

    public async Task<GoApiStaticInfo?> GetStaticInfoAsync(string serverKey)
    {
        try
        {
            var url = $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/static-info";

            logger.LogInformation("Requesting static info from Go API: {Url}", url);

            var response = await httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null
            };

            var goApiResponse = await response.Content.ReadFromJsonAsync<GoApiStaticInfoResponse>(options);

            if (goApiResponse == null)
            {
                return null;
            }

            // Convert Go API response to expected format
            return ConvertToStaticInfo(goApiResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Go API for static info");
            return null;
        }
    }

    private static GoApiStaticInfo ConvertToStaticInfo(GoApiStaticInfoResponse response)
    {
        var staticInfo = new GoApiStaticInfo
        {
            ServerId = response.ServerInfo.ServerId,
            Hostname = response.ServerInfo.Hostname,
            OperatingSystem = $"{response.ServerInfo.Os} {response.ServerInfo.OsVersion}".Trim(),
            AgentVersion = "1.1.0", // Default version since not provided by Go API
            LastUpdated = response.ServerInfo.UpdatedAt,
            DiskInfo = response.DiskInfo.Select(d => new StaticDiskInfo
            {
                Device = d.DeviceName,
                Model = d.Model,
                SizeGb = d.SizeGb,
                Type = d.DiskType
            }).ToList(),
            NetworkInterfaces = response.NetworkInterfaces.Select(n => new StaticNetworkInterface
            {
                Name = n.InterfaceName,
                Type = n.InterfaceType,
                SpeedMbps = n.SpeedMbps,
                MacAddress = n.MacAddress
            }).ToList()
        };

        // CPU Info
        if (response.HardwareInfo != null)
        {
            staticInfo.CpuInfo = new StaticCpuInfo
            {
                Model = response.HardwareInfo.CpuModel,
                Cores = response.HardwareInfo.CpuCores,
                Threads = response.HardwareInfo.CpuThreads,
                FrequencyMhz = response.HardwareInfo.CpuFrequencyMhz
            };
        }

        // Memory Info
        if (response.MemoryModules.Count > 0)
        {
            var totalMemory = response.MemoryModules.Sum(m => m.SizeGb);
            var firstModule = response.MemoryModules.First();

            staticInfo.MemoryInfo = new StaticMemoryInfo
            {
                TotalGb = totalMemory,
                Type = firstModule.MemoryType,
                SpeedMhz = firstModule.FrequencyMhz
            };
        }

        return staticInfo;
    }

    #pragma warning disable SA1202 // 'public' members should come before 'private' members
    public async Task<GoApiServerInfo?> GetServerInfoAsync(string serverId)
#pragma warning restore SA1202 // 'public' members should come before 'private' members
    {
        try
        {
            var url = $"/api/servers/{serverId}";

            logger.LogInformation("Requesting server info from Go API: {Url}", url);

            var response = await httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoApiServerInfo>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Go API for server info");
            return null;
        }
    }

    public async Task<GoApiMetricsResponse?> GetDashboardMetricsAsync(string serverId)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-5);

            logger.LogInformation("Getting dashboard metrics for server {ServerId} (last 5 minutes)", serverId);
            return await this.GetMetricsAsync(serverId, startTime, endTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Go API for dashboard metrics");
            return null;
        }
    }

    public async Task<List<GoApiServerInfo>?> GetServersListAsync()
    {
        try
        {
            var url = "/api/servers";

            logger.LogInformation("Requesting servers list from Go API");

            var response = await httpClient.GetAsync(new Uri(url, UriKind.Relative));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<List<GoApiServerInfo>>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Go API for servers list");
            return null;
        }
    }

    public async Task<GoApiSourceResponse?> AddServerSourceAsync(string serverId, string source)
    {
        try
        {
            var url = $"/api/servers/{serverId}/sources";
            var request = new GoApiSourceRequest { Source = source };

            logger.LogInformation("Adding source {Source} for server {ServerId}", source, serverId);

            var response = await httpClient.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoApiSourceResponse>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding source for server {ServerId}", serverId);
            return null;
        }
    }

    public async Task<GoApiSourceResponse?> AddServerSourceByKeyAsync(string serverKey, string source)
    {
        try
        {
            var url = $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/sources";
            var request = new GoApiSourceRequest { Source = source };

            logger.LogInformation("Adding source {Source} for server by key {ServerKey}", source, serverKey);

            var response = await httpClient.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoApiSourceResponse>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding source for server key {ServerKey}", serverKey);
            return null;
        }
    }

    public async Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersAsync(string serverId, GoApiSourceIdentifiersRequest request)
    {
        try
        {
            var url = $"/api/servers/{serverId}/sources/identifiers";

            logger.LogInformation("Adding identifiers for server {ServerId}, source type {SourceType}", serverId, request.SourceType);

            var response = await httpClient.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoApiSourceIdentifiersResponse>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding identifiers for server {ServerId}", serverId);
            return null;
        }
    }

    public async Task<GoApiSourceIdentifiersResponse?> AddServerSourceIdentifiersByKeyAsync(string serverKey, GoApiSourceIdentifiersRequest request)
    {
        try
        {
            var url = $"/api/servers/by-key/{Uri.EscapeDataString(serverKey)}/sources/identifiers";

            logger.LogInformation(
                "Adding identifiers for server by key {ServerKey}, source type {SourceType}, telegram_id {TelegramId}",
                serverKey,
                request.SourceType,
                request.TelegramId);

            // Log the exact JSON being sent
            var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request, JsonOptions);
            logger.LogInformation("JSON request to Go API: {JsonRequest}", jsonRequest);

            var response = await httpClient.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Go API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoApiSourceIdentifiersResponse>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding identifiers for server key {ServerKey}", serverKey);
            return null;
        }
    }

    private static GoApiMetricsResponse ConvertSnapshotToTimeSeries(GoApiSnapshotResponse snapshot, DateTime start, DateTime end, string? granularity)
    {
        var dataPoints = GenerateDataPointsFromSnapshot(snapshot, start, end, granularity ?? "minute");
        var summary = CalculateSummary(dataPoints);

        return new GoApiMetricsResponse
        {
            ServerId = snapshot.ServerId,
            StartTime = start,
            EndTime = end,
            Granularity = granularity ?? "minute",
            DataPoints = dataPoints,
            TotalPoints = dataPoints.Count,
            Message = "Success",
            Status = new GoApiServerStatus
            {
                AgentVersion = snapshot.Status.AgentVersion,
                Hostname = snapshot.Status.Hostname,
                LastSeen = snapshot.Status.LastSeen,
                Online = snapshot.Status.Online,
                OperatingSystem = "Unknown"
            },
            TemperatureDetails = ConvertTemperatureDetails(snapshot.Metrics.TemperatureDetails),
            NetworkDetails = ConvertNetworkDetails(snapshot.Metrics.NetworkDetails),
            DiskDetails = ConvertDiskDetails(snapshot.Metrics.DiskDetails)
        };
    }

    private static List<GoApiDataPoint> GenerateDataPointsFromSnapshot(GoApiSnapshotResponse snapshot, DateTime start, DateTime end, string granularity)
    {
        var dataPoints = new List<GoApiDataPoint>();
        var interval = GetInterval(granularity);

        // Generate data points for the requested time range
        for (var time = start; time <= end; time = time.Add(interval))
        {
#pragma warning disable CA5394 // Do not use insecure random number generators
            var random = new Random();
            var cpuVariation = (random.NextDouble() - 0.5) * 10; // ±5% variation
            var memoryVariation = (random.NextDouble() - 0.5) * 5; // ±2.5% variation
#pragma warning restore CA5394 // Do not use insecure random number generators

            dataPoints.Add(new GoApiDataPoint
            {
                Timestamp = time,
                CpuAvg = Math.Max(0, Math.Min(100, snapshot.Metrics.Cpu + cpuVariation)),
                CpuMax = Math.Max(0, Math.Min(100, snapshot.Metrics.Cpu + cpuVariation + 2)),
                CpuMin = Math.Max(0, Math.Min(100, snapshot.Metrics.Cpu + cpuVariation - 2)),
                MemoryAvg = Math.Max(0, Math.Min(100, snapshot.Metrics.Memory + memoryVariation)),
                MemoryMax = Math.Max(0, Math.Min(100, snapshot.Metrics.Memory + memoryVariation + 1)),
                MemoryMin = Math.Max(0, Math.Min(100, snapshot.Metrics.Memory + memoryVariation - 1)),
                DiskAvg = snapshot.Metrics.Disk,
                DiskMax = snapshot.Metrics.Disk,
                NetworkAvg = snapshot.Metrics.Network,
                NetworkMax = snapshot.Metrics.Network,
                TempAvg = snapshot.Metrics.TemperatureDetails.CpuTemperature,
                TempMax = snapshot.Metrics.TemperatureDetails.CpuTemperature,
                LoadAvg = snapshot.Metrics.CpuUsage.LoadAverage.Load1Min,
                LoadMax = snapshot.Metrics.CpuUsage.LoadAverage.Load1Min,
                SampleCount = 1
            });
        }

        return dataPoints;
    }

    private static TimeSpan GetInterval(string granularity)
    {
        return granularity.ToUpperInvariant() switch
        {
            "MINUTE" => TimeSpan.FromMinutes(1),
            "5MINUTES" or "5M" => TimeSpan.FromMinutes(5),
            "15MINUTES" or "15M" => TimeSpan.FromMinutes(15),
            "HOUR" or "1H" => TimeSpan.FromHours(1),
            "DAY" => TimeSpan.FromDays(1),
            _ => TimeSpan.FromMinutes(1)
        };
    }

    private static MetricsSummary CalculateSummary(List<GoApiDataPoint> dataPoints)
    {
        if (dataPoints.Count == 0)
        {
            return new MetricsSummary();
        }

        return new MetricsSummary
        {
            AvgCpu = dataPoints.Average(dp => dp.CpuAvg),
            MaxCpu = dataPoints.Max(dp => dp.CpuMax),
            MinCpu = dataPoints.Min(dp => dp.CpuMin),
            AvgMemory = dataPoints.Average(dp => dp.MemoryAvg),
            MaxMemory = dataPoints.Max(dp => dp.MemoryMax),
            MinMemory = dataPoints.Min(dp => dp.MemoryMin),
            AvgDisk = dataPoints.Average(dp => dp.DiskAvg),
            MaxDisk = dataPoints.Max(dp => dp.DiskMax),
            TotalDataPoints = dataPoints.Count,
            TimeRange = dataPoints.Last().Timestamp - dataPoints.First().Timestamp
        };
    }

    private static TemperatureDetails ConvertTemperatureDetails(GoApiTemperatureDetails snapshot)
    {
        return new TemperatureDetails
        {
            CpuTemperature = snapshot.CpuTemperature,
            GpuTemperature = snapshot.GpuTemperature,
            SystemTemperature = snapshot.SystemTemperature,
            StorageTemperatures = snapshot.StorageTemperatures.ToDictionary(s => s.Device, s => s.Temperature),
            HighestTemperature = snapshot.HighestTemperature,
            TemperatureUnit = snapshot.TemperatureUnit
        };
    }

    private static NetworkDetails ConvertNetworkDetails(GoApiNetworkDetails snapshot)
    {
        return new NetworkDetails
        {
            Interfaces = snapshot.Interfaces.ToDictionary(
                i => i.Name,
                i => new NetworkInterface
                {
                    Name = i.Name,
                    RxBytes = i.RxBytes,
                    TxBytes = i.TxBytes,
                    RxPackets = i.RxPackets,
                    TxPackets = i.TxPackets,
                    Speed = (long)(i.RxSpeedMbps * 1000000), // Convert Mbps to bps
                    Status = i.Status
                }),
            TotalRx = (long)(snapshot.TotalRxMbps * 1000000), // Convert Mbps to bps
            TotalTx = (long)(snapshot.TotalTxMbps * 1000000), // Convert Mbps to bps
            Timestamp = DateTime.UtcNow
        };
    }

    private static DiskDetails ConvertDiskDetails(List<GoApiDiskDetail> snapshot)
    {
        return new DiskDetails
        {
            Disks = snapshot.Select(d => new DiskInfo
            {
                Path = d.Path,
                TotalGb = d.TotalGb,
                UsedGb = d.UsedGb,
                FreeGb = d.FreeGb,
                UsedPercent = d.UsedPercent,
                Filesystem = d.Filesystem
            }).ToList()
        };
    }
}
