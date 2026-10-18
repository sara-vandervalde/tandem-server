using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Tandem.API.Middleware;
using Tandem.Core.Interfaces;
using Tandem.Data;
using Tandem.Data.Notifications;

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
