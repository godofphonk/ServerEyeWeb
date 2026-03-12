namespace ServerEye.Core.Services;

using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class StaticInfoService : IStaticInfoService
{
    private readonly IGoApiClient goApiClient;
    private readonly IUserServerAccessRepository accessRepository;
    private readonly ILogger<StaticInfoService> logger;

    public StaticInfoService(
        IGoApiClient goApiClient,
        IUserServerAccessRepository accessRepository,
        ILogger<StaticInfoService> logger)
    {
        this.goApiClient = goApiClient;
        this.accessRepository = accessRepository;
        this.logger = logger;
    }

    public async Task<GoApiStaticInfo?> GetStaticInfoAsync(Guid userId, string serverKey)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        this.logger.LogInformation(
            "[PERF] GetStaticInfoAsync started for server key {ServerKey}",
            serverKey);

        try
        {
            // Validate access by server key
            var accessCheckTime = System.Diagnostics.Stopwatch.StartNew();
            await this.ValidateAccessByServerKeyAsync(userId, serverKey);
            accessCheckTime.Stop();
            this.logger.LogInformation("[PERF] Access validation took {Ms}ms", accessCheckTime.ElapsedMilliseconds);

            // Get static info from Go API
            var goApiTime = System.Diagnostics.Stopwatch.StartNew();
            var staticInfo = await this.goApiClient.GetStaticInfoAsync(serverKey);
            goApiTime.Stop();

            if (staticInfo == null)
            {
                this.logger.LogWarning("[PERF] Go API returned null for static info after {Ms}ms", goApiTime.ElapsedMilliseconds);
                throw new InvalidOperationException("Failed to retrieve static info from Go API");
            }

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
            throw;
        }
    }

    private async Task ValidateAccessByServerKeyAsync(Guid userId, string serverKey)
    {
        // First validate the server key and get server info
        var serverInfo = await this.goApiClient.ValidateServerKeyAsync(serverKey);
        #pragma warning disable IDE0270 // Simplify null check
        if (serverInfo is null)
#pragma warning restore IDE0270 // Simplify null check
        {
            throw new UnauthorizedAccessException("Invalid server key");
        }

        // Then check user access to this server
        var hasAccess = await this.accessRepository.HasAccessAsync(userId, serverInfo.ServerId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("You don't have access to this server");
        }
    }
}
