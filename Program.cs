using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VideoManager.Api.Auth;
using VideoManager.Api.Data;
using VideoManager.Api.Helpers;
using VideoManager.Api.Middleware;
using VideoManager.Api.Repositories;
using VideoManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VideoManager API", Version = "v1" });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Description = "Please insert the token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
});

// In-Memory Database
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("VideoDB"));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    opt.Password.RequiredLength = 8;
    opt.Password.RequireNonAlphanumeric = false;
    opt.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();


// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o =>
  {
      var secret = JwtKeyHelper.GetSecretBytes(builder.Configuration);
      o.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidateLifetime = true,
          ValidIssuer = builder.Configuration["Jwt:Issuer"],
          ValidAudience = builder.Configuration["Jwt:Audience"],
          IssuerSigningKey = new SymmetricSecurityKey(secret),
          ClockSkew = TimeSpan.FromSeconds(30)
      };

       o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers.Authorization.ToString();
                var log = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JWT");
                log.LogInformation("Authorization header present? {has}", !string.IsNullOrEmpty(auth));

                if (string.IsNullOrEmpty(ctx.Token) && ctx.Request.Cookies.TryGetValue("access_token", out var t))
                            ctx.Token = t;
                        return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("JWT")
                   .LogError(ctx.Exception, "JWT auth failed"); 
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("JWT")
                   .LogWarning("JWT challenge. Error:{Error} Desc:{Desc}", ctx.Error, ctx.ErrorDescription);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var sub = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("JWT").LogInformation("JWT token validated for {sub}", sub ?? "(null)");
                return Task.CompletedTask;
            }
        };
  });


builder.Services.AddAuthorization();

//CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("default", p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

// Rate Limiter 
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromSeconds(30);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
    });
});

// DI
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<ITextDetectionService, TextDetectionService>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();


var app = builder.Build();

// Dev Exception Page
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); 
}

// Detailed errors for EF Core
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

// Custom Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("default");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
