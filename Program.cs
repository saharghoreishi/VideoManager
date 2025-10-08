using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VideoManager.Api.Auth;
using VideoManager.Api.Data;
using VideoManager.Api.Repositories;
using VideoManager.Api.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-Memory Database
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("VideoDB"));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    opt.Password.RequiredLength = 8;
    opt.Password.RequireNonAlphanumeric = false;
    opt.SignIn.RequireConfirmedEmail = false;
}).AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();


// Dependency Injection
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<ITextDetectionService, TextDetectionService>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Add services to the container.
builder.Services.AddControllers();

// AuthN/Z (.NET 8 Identity + JwtBearer tokens)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://login.microsoftonline.com/<TENANT_ID>/v2.0"; // falls Azure AD
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = "<DEINE_API_CLIENT_ID_oder_APP_ID_URI>",
            ValidateIssuer = true
        };
        // Für bessere Fehlermeldungen:
        options.IncludeErrorDetails = true;
        options.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                // zeigt "error" und "error_description" im WWW-Authenticate
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.Headers.Append("WWW-Authenticate",
                    $"Bearer error=\"invalid_token\", error_description=\"{ctx.ErrorDescription}\"");
                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization();

// Rate limiting 
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromSeconds(30);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("default", p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization(); 
app.UseAuthentication();


app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
