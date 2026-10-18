using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Tandem.API.Middleware;
using Tandem.Core.Interfaces;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;
using Tandem.Data;
using Tandem.Data.Notifications;
using Tandem.Data.Repositories;
using Tandem.Service.Authentication;
using Tandem.Service.Mapping;
using Tandem.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Code logs through ILogger only; NLog writes it to the console and to a daily file (see nlog.config).
// RemoveLoggerFactoryFilter = false keeps the log levels from appsettings, so they can differ per environment
builder.Logging.ClearProviders();
builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

// Add services to the container.

// The connection string comes from User Secrets in development - never from code or appsettings.json
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing. Set it with dotnet user-secrets.");

// Registered as Scoped: one DbContext per HTTP request
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Repositories: Scoped, like the DbContext they use
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT settings through IOptions. The signing key is a secret (User Secrets), so fail fast if it is missing or too short
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtKey = jwtSection["Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters. Set it with dotnet user-secrets.");
}
builder.Services.Configure<JwtSettings>(jwtSection);

// Stateless helpers, safe to share across all requests
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddAutoMapper(typeof(UserProfile));

// The notification channel is chosen in appsettings, so switching to email or SMS needs no code change
var notificationChannel = builder.Configuration["Notifications:Channel"];
switch (notificationChannel)
{
    case "Log":
        builder.Services.AddScoped<INotificationSender, LogNotificationSender>();
        break;
    default:
        throw new InvalidOperationException($"Unknown notification channel '{notificationChannel}'.");
}

builder.Services.AddControllers();

// Swagger: scans the controllers and builds a documentation page for every endpoint
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Must be first, so it catches exceptions from everything that runs after it
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Swagger is shown only in development, not in production
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
