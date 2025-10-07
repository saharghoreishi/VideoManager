using Microsoft.EntityFrameworkCore;
using VideoManager.Data;
using VideoManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-Memory Database
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("VideoDB"));
// Dependency Injection
builder.Services.AddScoped<IVideoService, VideoService>();
// Add services to the container.
builder.Services.AddControllers();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
