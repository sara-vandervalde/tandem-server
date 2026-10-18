namespace Tandem.Core.DTOs.Groups;

public class GroupMemberDto
{
    public int UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTime JoinedAt { get; init; }
}
