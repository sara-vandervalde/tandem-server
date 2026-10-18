using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Auth;
using Tandem.Core.DTOs.Users;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;

namespace Tandem.Service.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
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
}
