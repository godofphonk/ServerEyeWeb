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

    public async Task<UserData> CreateUserAsync(UserRequestDto requestDto)
    {
        ArgumentNullException.ThrowIfNull(requestDto);
        var user = new User()
        {
            Email = requestDto.Email,
            UserName = requestDto.UserName,
            Password = requestDto.Password,
        };
        await this.userRepository.AddAsync(user);
        return new UserData
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
        };
    }

    public async Task<UserData> UpdateUserAsync(UserRequestDto requestDto)
    {
        ArgumentNullException.ThrowIfNull(requestDto);
        var existingUser = await this.userRepository
                               .GetByIdAsync(requestDto.UserId)
                           ?? throw new KeyNotFoundException($"User with ID {requestDto.UserId} not found");

        existingUser.UserName = requestDto.UserName;
        existingUser.Email = requestDto.Email;
        existingUser.Password = requestDto.Password;
        existingUser.ServerId = requestDto.ServerId;

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
}
