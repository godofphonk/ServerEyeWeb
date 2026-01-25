namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs;
using ServerEye.Core.DTOs.UserDto;

public interface IUserService
{
    public Task<UserData?> GetUserByIdAsync(Guid id);
    public Task<UserData?> GetUserByEmailAsync(string email);
    public Task<List<UserData>> GetAllUsersAsync();
    public Task<UserData> CreateUserAsync(UserRegisterDto userRegisterDto);
    public Task<UserData> UpdateUserAsync(Guid id, UserUpdateDto updateDto);
    public Task DeleteUserAsync(Guid id);
    public Task<UserData> LoginUserAsync(UserLoginDto userLoginDto);
}
