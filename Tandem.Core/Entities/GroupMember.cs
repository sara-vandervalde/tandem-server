namespace Tandem.Core.Entities;

// Join table between Users and Groups. The key is (GroupId, UserId) - defined in the Configuration
public class GroupMember
{
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
