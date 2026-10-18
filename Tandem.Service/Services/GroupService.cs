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
            return Result<GroupDetailsDto>.NotFound("הקבוצה לא נמצאה");
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
}
