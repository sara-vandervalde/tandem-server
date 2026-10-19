using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Groups;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;

namespace Tandem.Service.Services;

public class GroupService : IGroupService
{
    private const string GroupNotFoundMessage = "הקבוצה לא נמצאה";
    private const string ManagerOnlyMessage = "רק מנהל הקבוצה יכול לבצע פעולה זו";

    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GroupService> _logger;

    public GroupService(IGroupRepository groupRepository, IMapper mapper, ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GroupDto>> GetGroupsAsync(int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var groups = isAdmin
            ? await _groupRepository.GetAllAsync(cancellationToken)
            : await _groupRepository.GetForUserAsync(userId, cancellationToken);

        return _mapper.Map<List<GroupDto>>(groups);
    }

    public async Task<Result<GroupDetailsDto>> GetByIdAsync(
        int groupId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetDetailsAsync(groupId, cancellationToken);
        if (group is null)
        {
            return Result<GroupDetailsDto>.NotFound(GroupNotFoundMessage);
        }

        // Members see their own groups; the Admin sees every group
        if (!isAdmin && group.Members.All(m => m.UserId != userId))
        {
            return Result<GroupDetailsDto>.Forbidden("אין לך גישה לקבוצה זו");
        }

        return Result<GroupDetailsDto>.Success(_mapper.Map<GroupDetailsDto>(group));
    }

    public async Task<Result<GroupDetailsDto>> CreateAsync(
        CreateGroupRequest request, int userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // The creator becomes the manager and the first member - saved together, in one SaveChanges
        var group = new Group
        {
            Name = request.Name.Trim(),
            ManagerId = userId,
            CreatedAt = now
        };
        group.Members.Add(new GroupMember { UserId = userId, JoinedAt = now });

        _groupRepository.Add(group);
        await _groupRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Group {GroupId} created by user {UserId}", group.Id, userId);

        // Reloaded with the manager's and member's names for the response
        var created = await _groupRepository.GetDetailsAsync(group.Id, cancellationToken);
        return Result<GroupDetailsDto>.Success(_mapper.Map<GroupDetailsDto>(created!));
    }

    public async Task<Result> UpdateAsync(
        int groupId, UpdateGroupRequest request, int userId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersForUpdateAsync(groupId, cancellationToken);
        if (group is null)
        {
            return Result.NotFound(GroupNotFoundMessage);
        }

        if (group.ManagerId != userId)
        {
            return Result.Forbidden(ManagerOnlyMessage);
        }

        group.Name = request.Name.Trim();
        await _groupRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int groupId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersForUpdateAsync(groupId, cancellationToken);
        if (group is null)
        {
            return Result.NotFound(GroupNotFoundMessage);
        }

        if (!isAdmin && group.ManagerId != userId)
        {
            return Result.Forbidden("רק מנהל הקבוצה או מנהל המערכת יכולים למחוק קבוצה");
        }

        _groupRepository.Remove(group);
        await _groupRepository.SaveChangesAsync(cancellationToken);

        // Warning: deleting a group removes all its events - worth noticing in the log
        _logger.LogWarning("Group {GroupId} deleted by user {UserId}", groupId, userId);

        return Result.Success();
    }

    public async Task<Result> TransferManagerAsync(
        int groupId, int newManagerId, int userId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersForUpdateAsync(groupId, cancellationToken);
        if (group is null)
        {
            return Result.NotFound(GroupNotFoundMessage);
        }

        if (group.ManagerId != userId)
        {
            return Result.Forbidden(ManagerOnlyMessage);
        }

        if (group.Members.All(m => m.UserId != newManagerId))
        {
            return Result.ValidationError("אפשר להעביר את הניהול רק לחבר/ה בקבוצה");
        }

        // Transferring to the current manager changes nothing and is not an error
        if (group.ManagerId != newManagerId)
        {
            group.ManagerId = newManagerId;
            await _groupRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Group {GroupId} manager changed from {OldManagerId} to {NewManagerId}",
                groupId, userId, newManagerId);
        }

        return Result.Success();
    }

    public async Task<Result> RemoveMemberAsync(int groupId, int memberId, int userId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersForUpdateAsync(groupId, cancellationToken);
        if (group is null)
        {
            return Result.NotFound(GroupNotFoundMessage);
        }

        var membership = group.Members.FirstOrDefault(m => m.UserId == memberId);
        if (membership is null)
        {
            return Result.NotFound("החבר/ה לא נמצא/ה בקבוצה");
        }

        var isLeaving = memberId == userId;
        if (!isLeaving && group.ManagerId != userId)
        {
            return Result.Forbidden(ManagerOnlyMessage);
        }

        // The group must always have a manager
        if (memberId == group.ManagerId)
        {
            return group.Members.Count == 1
                ? Result.Conflict("את/ה החבר/ה היחיד/ה בקבוצה. אפשר רק למחוק את הקבוצה")
                : Result.Conflict("יש להעביר את ניהול הקבוצה לחבר אחר לפני העזיבה");
        }

        if (await _groupRepository.HasOpenResponsibilitiesAsync(groupId, memberId, cancellationToken))
        {
            return Result.Conflict("יש לשחרר את האחריות באירועים פתוחים לפני העזיבה");
        }

        // Leaving the group and leaving the open events' cost sharing are saved together
        group.Members.Remove(membership);
        await _groupRepository.RemoveFromOpenEventParticipantsAsync(groupId, memberId, cancellationToken);
        await _groupRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {MemberId} {Action} group {GroupId}",
            memberId, isLeaving ? "left" : $"was removed by {userId} from", groupId);

        return Result.Success();
    }
}
