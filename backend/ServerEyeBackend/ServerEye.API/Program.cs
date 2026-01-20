using Microsoft.EntityFrameworkCore;
using ServerEye.Infrastracture;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

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
