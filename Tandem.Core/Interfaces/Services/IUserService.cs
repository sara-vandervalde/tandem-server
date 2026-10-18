using Tandem.Core.Common;
using Tandem.Core.DTOs.Common;
using Tandem.Core.DTOs.Users;

namespace Tandem.Core.Interfaces.Services;

// System administration of users (Admin role only - enforced by the controller)
public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(PaginationQuery query, CancellationToken cancellationToken);

    Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result> SetActiveAsync(int id, bool isActive, int currentUserId, CancellationToken cancellationToken);
}
