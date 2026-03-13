namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.GoApi;
using ServerEye.Core.DTOs.Server;

public interface ISourceManagementService
{
    /// <summary>
    /// Deletes a source from a server by server key.
    /// </summary>
    public Task<DeleteSourceResponseDto> DeleteServerSourceAsync(Guid userId, string serverKey, string source);

    /// <summary>
    /// Deletes specific identifiers from a server by server key.
    /// </summary>
    public Task<DeleteSourceIdentifiersResponseDto> DeleteServerSourceIdentifiersAsync(Guid userId, string serverKey, DeleteSourceIdentifiersRequestDto request);

    /// <summary>
    /// Deletes specific identifiers from a server by server key and source type.
    /// </summary>
    public Task<DeleteSourceIdentifiersResponseDto> DeleteServerSourceIdentifiersByTypeAsync(Guid userId, string serverKey, string sourceType, DeleteSourceIdentifiersRequestDto request);
}
