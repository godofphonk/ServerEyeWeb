namespace ServerEye.Core.Services;

using ServerEye.Core.DTOs;
using ServerEye.Core.DTOs.UserDto;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Core.Interfaces.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    private readonly IUserRepository userRepository = userRepository;

    public async Task<UserData?> GetUserByIdAsync(Guid id)
    {
        var user = await this.userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }
        return new UserData()
        {
            Email = user.Email,
            Id = user.Id,
            UserName = user.UserName,
            ServerId = user.ServerId,
        };
    }

    public async Task<UserData?> GetUserByEmailAsync(string email)
    {
        var user = await this.userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return null;
        }
        return new UserData()
        {
            Email = user.Email,
            Id = user.Id,
            UserName = user.UserName,
            ServerId = user.ServerId,
        };
    }

    public async Task<List<UserData>> GetAllUsersAsync()
    {
        var users = await this.userRepository.GetAllAsync();

        return users.Select(user => new UserData
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            ServerId = user.ServerId,
        }).ToList();
    }

    public async Task<UserData> CreateUserAsync(UserRegisterDto userRegisterDto)
    {
        ArgumentNullException.ThrowIfNull(userRegisterDto);
        var user = new User()
        {
            Email = userRegisterDto.Email,
            UserName = userRegisterDto.UserName,
            Password = userRegisterDto.Password,
        };
        await this.userRepository.AddAsync(user);
        return new UserData
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
        };
    }

    public async Task<UserData> UpdateUserAsync(Guid id, UserUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);
        var existingUser = await this.userRepository
                               .GetByIdAsync(id)
                           ?? throw new KeyNotFoundException($"User with ID {id} not found");

        existingUser.UserName = updateDto.UserName;
        existingUser.Email = updateDto.Email;
        existingUser.Password = updateDto.Password;
        existingUser.ServerId = updateDto.ServerId;

        await this.userRepository.UpdateUserAsync(existingUser);

        return new UserData
        {
            Id = existingUser.Id,
            UserName = existingUser.UserName,
            Email = existingUser.Email,
            ServerId = existingUser.ServerId,
        };
    }

    public async Task DeleteUserAsync(Guid id) => await this.userRepository.DeleteAsync(id);

    public async Task<UserData> LoginUserAsync(UserLoginDto userLoginDto)
    {
        ArgumentNullException.ThrowIfNull(userLoginDto);

        var ifuserexist = await this.userRepository.GetByEmailAsync(userLoginDto.Email) ??
                              throw new KeyNotFoundException($"User with email {userLoginDto.Email} not found");

        if (ifuserexist.Password != userLoginDto.Password)
        {
            throw new KeyNotFoundException($"User with email {userLoginDto.Email} not found");
        }
        return new UserData()
        {
            Id = ifuserexist.Id,
            Email = ifuserexist.Email,
            UserName = ifuserexist.UserName,
            ServerId = ifuserexist.ServerId,
        };
    }
}
