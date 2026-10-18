namespace Tandem.Core.DTOs.Groups;

// One group's page: the group and its members
public class GroupDetailsDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int ManagerId { get; init; }

    public string ManagerName { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<GroupMemberDto> Members { get; init; } = [];
}
