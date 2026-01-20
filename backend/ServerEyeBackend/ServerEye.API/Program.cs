using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Interfaces.Repository;
using ServerEye.Infrastracture;
using ServerEye.Infrastracture.Repositories;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddScoped<IServerRepository, ServerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ServerEyeDbContext>(
    options =>
        options.UseNpgsql(configuration.GetConnectionString(nameof(ServerEyeDbContext))));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
