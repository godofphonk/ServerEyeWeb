namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class StaticInfoService : IStaticInfoService
{
    private readonly IGoApiClient goApiClient;
    private readonly IUserServerAccessRepository accessRepository;
    private readonly ILogger<StaticInfoService> logger;
    private readonly IMetricsCacheService cacheService;
    private readonly CacheSettings cacheSettings;

    public StaticInfoService(
        IGoApiClient goApiClient,
        IUserServerAccessRepository accessRepository,
        ILogger<StaticInfoService> logger,
        IMetricsCacheService cacheService,
        CacheSettings cacheSettings)
    {
        this.goApiClient = goApiClient;
        this.accessRepository = accessRepository;
        this.logger = logger;
        this.cacheService = cacheService;
        this.cacheSettings = cacheSettings;
    }

    public async Task<GoApiStaticInfo?> GetStaticInfoAsync(Guid userId, string serverKey)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        this.logger.LogInformation(
            "[PERF] GetStaticInfoAsync started for server key {ServerKey}",
            serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");

        try
        {
            var cacheKey = $"staticInfo:{serverKey}";
            var cachedResult = await this.cacheService.GetAsync<GoApiStaticInfo>(cacheKey);

            if (cachedResult != null)
            {
                stopwatch.Stop();
                this.logger.LogInformation("[PERF] Cache hit for static info: {ServerKey}, completed in {Ms}ms", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null", stopwatch.ElapsedMilliseconds);
                return cachedResult;
            }

            // Validate access by server key
            var accessCheckTime = System.Diagnostics.Stopwatch.StartNew();
            var serverInfo = await this.goApiClient.ValidateServerKeyAsync(serverKey ?? string.Empty);
            accessCheckTime.Stop();

            // If server not found in Go API (404), return null
            if (serverInfo == null)
            {
                this.logger.LogWarning("[PERF] Server not found in Go API for key {ServerKey} (404), returning null", serverKey?.Replace("\r", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? "null");
                stopwatch.Stop();
                return null;
            }

            // Check user access to this server
            var hasAccess = await this.accessRepository.HasAccessAsync(userId, serverInfo.ServerId);
            if (!hasAccess)
            {
                this.logger.LogWarning("[PERF] User {UserId} does not have access to server {ServerId}", userId, serverInfo.ServerId);
                stopwatch.Stop();
                return null;
            }

            this.logger.LogInformation("[PERF] Access validation took {Ms}ms", accessCheckTime.ElapsedMilliseconds);

            // Get static info from Go API
            var goApiTime = System.Diagnostics.Stopwatch.StartNew();
            var staticInfo = await this.goApiClient.GetStaticInfoAsync(serverKey ?? string.Empty);
            goApiTime.Stop();

            if (staticInfo == null)
            {
                this.logger.LogWarning("[PERF] Go API returned null for static info after {Ms}ms", goApiTime.ElapsedMilliseconds);
                stopwatch.Stop();
                return null;
            }

            // Get server status to retrieve agent version
            GoApiServerStatus? serverStatus = null;
            try
            {
                serverStatus = await this.goApiClient.GetServerStatusAsync(serverKey ?? string.Empty);
                if (serverStatus?.AgentVersion != null)
                {
                    this.logger.LogInformation("[PERF] Got agent version {Version} from status endpoint", serverStatus.AgentVersion);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "[PERF] Failed to get server status for agent version");
            }

            // Update agent version if available from status
            if (serverStatus?.AgentVersion != null && staticInfo.AgentVersion != serverStatus.AgentVersion)
            {
                staticInfo = new GoApiStaticInfo
                {
                    ServerId = staticInfo.ServerId,
                    Hostname = staticInfo.Hostname,
                    OperatingSystem = staticInfo.OperatingSystem,
                    Kernel = staticInfo.Kernel,
                    Architecture = staticInfo.Architecture,
                    AgentVersion = serverStatus.AgentVersion,
                    LastUpdated = staticInfo.LastUpdated,
                    CpuInfo = staticInfo.CpuInfo,
                    MemoryInfo = staticInfo.MemoryInfo,
                    DiskInfo = staticInfo.DiskInfo,
                    NetworkInterfaces = staticInfo.NetworkInterfaces,
                    MotherboardInfo = staticInfo.MotherboardInfo,
                    GpuInfo = staticInfo.GpuInfo
                };
            }

            await this.cacheService.SetAsync(cacheKey, staticInfo, this.cacheSettings.StaticInfo);

            stopwatch.Stop();
            this.logger.LogInformation(
                "[PERF] GetStaticInfoAsync completed in {TotalMs}ms (access: {AccessMs}ms, goApi: {GoApiMs}ms) for server {ServerId}",
                stopwatch.ElapsedMilliseconds,
                accessCheckTime.ElapsedMilliseconds,
                goApiTime.ElapsedMilliseconds,
                staticInfo.ServerId);

            return staticInfo;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.logger.LogError(ex, "[PERF] Error in GetStaticInfoAsync after {Ms}ms", stopwatch.ElapsedMilliseconds);

            // Return null on error instead of throwing to prevent infinite loading
            return null;
        }
    }
}
