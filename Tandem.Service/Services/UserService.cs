using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Common;
using Tandem.Core.DTOs.Users;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;

namespace Tandem.Service.Services;

public class UserService : IUserService
{
    private const string UserNotFoundMessage = "המשתמש לא נמצא";

    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, IMapper mapper, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(PaginationQuery query, CancellationToken cancellationToken)
    {
        var page = await _userRepository.GetPagedAsync(query.Page, query.PageSize, cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = _mapper.Map<List<UserDto>>(page.Items),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);

        return user is null
            ? Result<UserDto>.NotFound(UserNotFoundMessage)
            : Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result> SetActiveAsync(int id, bool isActive, int currentUserId, CancellationToken cancellationToken)
    {
        // An admin who blocks themselves could lock everyone out of the admin screens
        if (id == currentUserId && !isActive)
        {
            return Result.ValidationError("לא ניתן לחסום את המשתמש שלך");
        }

        var user = await _userRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (user is null)
        {
            return Result.NotFound(UserNotFoundMessage);
        }

        // Setting the same value again is not an error (idempotent PATCH)
        if (user.IsActive != isActive)
        {
            user.IsActive = isActive;
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} was {Action} by admin {AdminId}",
                id, isActive ? "unblocked" : "blocked", currentUserId);
        }

        return Result.Success();
    }
}
