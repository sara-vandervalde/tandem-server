using Tandem.Core.Common;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Service.Authorization;

// All group-level permission checks in one place, instead of repeated in every service.
// They are not JWT roles, because the same user can manage one group and be a regular member of another
public class GroupAccessService
{
    private readonly IGroupRepository _groupRepository;

    public GroupAccessService(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    // 404 if the group does not exist, 403 if the user is not its manager
    public async Task<Result<Group>> EnsureManagerAsync(int groupId, int userId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            return Result<Group>.NotFound("הקבוצה לא נמצאה");
        }

        return group.ManagerId == userId
            ? Result<Group>.Success(group)
            : Result<Group>.Forbidden("רק מנהל הקבוצה יכול לבצע פעולה זו");
    }

    // 404 if the group does not exist, 403 if the user is neither a member nor an Admin
    public async Task<Result<Group>> EnsureMemberOrAdminAsync(
        int groupId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            return Result<Group>.NotFound("הקבוצה לא נמצאה");
        }

        if (isAdmin || await _groupRepository.IsMemberAsync(groupId, userId, cancellationToken))
        {
            return Result<Group>.Success(group);
        }

        return Result<Group>.Forbidden("אין לך גישה לקבוצה זו");
    }
}
