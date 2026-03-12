namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs;

public interface IMockDataProvider
{
    public Task<ServersResponseDto> GetMockServersAsync();
}
