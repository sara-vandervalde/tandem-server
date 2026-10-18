using Tandem.Core.Common;
using Tandem.Core.DTOs.Groups;

namespace Tandem.Core.Interfaces.Services;

public interface IGroupService
{
    // Admin: every group in the system. Member: only the groups they belong to
    Task<IReadOnlyList<GroupDto>> GetGroupsAsync(int userId, bool isAdmin, CancellationToken cancellationToken);

    Task<Result<GroupDetailsDto>> GetByIdAsync(int groupId, int userId, bool isAdmin, CancellationToken cancellationToken);

    Task<Result<GroupDetailsDto>> CreateAsync(CreateGroupRequest request, int userId, CancellationToken cancellationToken);
}
