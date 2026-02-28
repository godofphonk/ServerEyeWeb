namespace ServerEye.UnitTests.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Infrastracture;
using ServerEye.Infrastracture.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ServerEyeDbContext context;
    private readonly UserRepository sut;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ServerEyeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        this.context = new ServerEyeDbContext(options);
        this.sut = new UserRepository(this.context);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = Core.Enums.UserRole.Admin
        };
        await this.context.Users.AddAsync(user);
        await this.context.SaveChangesAsync();

        var result = await this.sut.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.UserName.Should().Be(user.UserName);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await this.sut.GetByIdAsync(nonExistentId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ShouldReturnUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = Core.Enums.UserRole.Admin
        };
        await this.context.Users.AddAsync(user);
        await this.context.SaveChangesAsync();

        var result = await this.sut.GetByEmailAsync(user.Email);

        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var result = await this.sut.GetByEmailAsync("nonexistent@example.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "newuser",
            Email = "new@example.com",
            Password = "hashedpassword",
            Role = Core.Enums.UserRole.Admin
        };

        await this.sut.AddAsync(user);

        var savedUser = await this.context.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.UserName.Should().Be(user.UserName);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@example.com", Password = "hash1", Role = Core.Enums.UserRole.User },
            new() { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@example.com", Password = "hash2", Role = Core.Enums.UserRole.User },
            new() { Id = Guid.NewGuid(), UserName = "user3", Email = "user3@example.com", Password = "hash3", Role = Core.Enums.UserRole.Admin }
        };

        await this.context.Users.AddRangeAsync(users);
        await this.context.SaveChangesAsync();

        var result = await this.sut.GetAllAsync();

        result.Should().HaveCount(3);
        result.Should().Contain(u => u.UserName == "user1");
        result.Should().Contain(u => u.UserName == "user2");
        result.Should().Contain(u => u.UserName == "user3");
    }

    [Fact]
    public async Task GetUsersWithPaginationAsync_ShouldReturnCorrectPage()
    {
        var users = Enumerable.Range(1, 10).Select(i => new User
        {
            Id = Guid.NewGuid(),
            UserName = $"user{i}",
            Email = $"user{i}@example.com",
            Password = "hash",
            Role = Core.Enums.UserRole.Admin
        }).ToList();

        await this.context.Users.AddRangeAsync(users);
        await this.context.SaveChangesAsync();

        var result = await this.sut.GetUsersWithPaginationAsync(page: 2, pageSize: 3);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUserFromDatabase()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "deleteuser",
            Email = "delete@example.com",
            Password = "hash",
            Role = Core.Enums.UserRole.Admin
        };
        await this.context.Users.AddAsync(user);
        await this.context.SaveChangesAsync();

        var userToDelete = await this.context.Users.FindAsync(user.Id);
        if (userToDelete != null)
        {
            this.context.Users.Remove(userToDelete);
            await this.context.SaveChangesAsync();
        }

        var deletedUser = await this.context.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    public void Dispose()
    {
        this.context.Database.EnsureDeleted();
        this.context.Dispose();
        GC.SuppressFinalize(this);
    }
}
