using Tandem.Core.Common;
using Tandem.Core.DTOs.Groups;

namespace Tandem.Core.Interfaces.Services;

public interface IGroupService
{
    // Admin: every group in the system. Member: only the groups they belong to
    Task<IReadOnlyList<GroupDto>> GetGroupsAsync(int userId, bool isAdmin, CancellationToken cancellationToken);

    Task<Result<GroupDetailsDto>> GetByIdAsync(int groupId, int userId, bool isAdmin, CancellationToken cancellationToken);

    Task<Result<GroupDetailsDto>> CreateAsync(CreateGroupRequest request, int userId, CancellationToken cancellationToken);

    // Manager only
    Task<Result> UpdateAsync(int groupId, UpdateGroupRequest request, int userId, CancellationToken cancellationToken);

    // Manager or Admin. Deletes the group with everything in it
    Task<Result> DeleteAsync(int groupId, int userId, bool isAdmin, CancellationToken cancellationToken);

    // Manager only; the new manager must already be a member. The previous manager stays as a regular member
    Task<Result> TransferManagerAsync(int groupId, int newManagerId, int userId, CancellationToken cancellationToken);

    // A member leaving (memberId == userId) or the manager removing someone.
    // The manager cannot leave before transferring; nobody leaves with open responsibilities: 409
    Task<Result> RemoveMemberAsync(int groupId, int memberId, int userId, CancellationToken cancellationToken);
}
