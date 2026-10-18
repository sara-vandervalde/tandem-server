using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Tandem.Service.Authentication;

namespace Tandem.API.Extensions;

public static class AuthenticationExtensions
{
    // Registers JwtSettings (IOptions) and validates the token sent in the Authorization header of every request
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var settings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

        // The signing key is a secret (User Secrets), so fail fast if it is missing or too short for HMAC-SHA256
        if (string.IsNullOrEmpty(settings.Key) || settings.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 characters. Set it with dotnet user-secrets.");
        }

        services.Configure<JwtSettings>(jwtSection);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Exactly the values JwtTokenGenerator used, so only our own, unexpired, unmodified tokens pass
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                // By default 401 and 403 have an empty body; return the same ProblemDetails JSON as every other error
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        // Why the request was rejected (missing, expired, bad signature...) - never the token itself
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>().CreateLogger("Authentication");
                        logger.LogWarning("Authentication failed for {Path}: {Reason}",
                            context.Request.Path,
                            context.AuthenticateFailure?.Message ?? "no token was sent");

                        context.HandleResponse();
                        await WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                            "יש להתחבר כדי לבצע פעולה זו");
                    },
                    OnForbidden = context => WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                        "אין לך הרשאה לבצע פעולה זו")
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static Task WriteProblemAsync(HttpContext httpContext, int statusCode, string title)
    {
        httpContext.Response.StatusCode = statusCode;
        var problem = new ProblemDetails { Status = statusCode, Title = title, Instance = httpContext.Request.Path };
        return httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
