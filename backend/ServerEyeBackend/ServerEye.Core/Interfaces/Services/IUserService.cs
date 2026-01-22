namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs;
using ServerEye.Core.DTOs.UserDto;

public interface IUserService
{
    public Task<UserData?> GetUserByIdAsync(Guid id);
    public Task<UserData?> GetUserByEmailAsync(string email);
    public Task<List<UserData>> GetAllUsersAsync();
    public Task<UserData> CreateUserAsync(UserRequestDto requestDto);
    public Task<UserData> UpdateUserAsync(UserRequestDto requestDto);
    public Task DeleteUserAsync(Guid id);
}
