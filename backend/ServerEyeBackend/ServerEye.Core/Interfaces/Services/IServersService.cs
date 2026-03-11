namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs;

public interface IServersService
{
    public Task<ServersResponseDto> GetUserServersAsync(Guid userId);
    public Task<ServersResponseDto> GetMockServersAsync();
}
