namespace Tandem.Core.DTOs.Groups;

// A group in a list ("my groups")
public class GroupDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int ManagerId { get; init; }

    public string ManagerName { get; init; } = string.Empty;

    public int MemberCount { get; init; }

    public DateTime CreatedAt { get; init; }
}
