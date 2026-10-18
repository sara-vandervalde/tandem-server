using Microsoft.EntityFrameworkCore;
using Tandem.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// The connection string comes from User Secrets in development - never from code or appsettings.json
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing. Set it with dotnet user-secrets.");

// Registered as Scoped: one DbContext per HTTP request
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddControllers();

// Swagger: scans the controllers and builds a documentation page for every endpoint
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
