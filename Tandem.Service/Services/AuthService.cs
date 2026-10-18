using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Auth;
using Tandem.Core.DTOs.Users;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Interfaces;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;

namespace Tandem.Service.Services;

public class AuthService : IAuthService
{
    // Same message for an unknown email and a wrong password, so nobody can probe which emails are registered
    private const string InvalidCredentialsMessage = "האימייל או הסיסמה שגויים";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<UserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        // Stored in lowercase so that Sara@... and sara@... are the same user
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return Result<UserDto>.Conflict("כתובת האימייל כבר רשומה במערכת");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Role = UserRole.Member,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _userRepository.Add(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Only the id - never the email, password or request body
        _logger.LogInformation("User {UserId} registered", user.Id);

        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login failed: unknown email");
            return Result<AuthResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: wrong password for user {UserId}", user.Id);
            return Result<AuthResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        // Checked only after the password, so a blocked account is not revealed to someone without the password
        if (!user.IsActive)
        {
            _logger.LogWarning("Login refused: user {UserId} is blocked", user.Id);
            return Result<AuthResponse>.Forbidden("החשבון חסום. יש לפנות למנהל המערכת");
        }

        var (token, expiresAt) = _tokenGenerator.Generate(user);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        });
    }
}
