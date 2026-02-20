namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.GoApi;

public interface IStaticInfoService
{
    public Task<GoApiStaticInfo?> GetStaticInfoAsync(Guid userId, string serverKey);
}
