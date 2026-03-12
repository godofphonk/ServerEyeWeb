namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Server;

public interface IServerDiscoveryService
{
    public Task<DiscoveredServersResponseDto> FindServersByTelegramIdAsync(Guid userId, long telegramId);
    public Task<ImportServersResponseDto> ImportDiscoveredServersAsync(Guid userId, List<string> serverIds);
}
