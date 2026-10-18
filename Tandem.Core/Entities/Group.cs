namespace Tandem.Core.Entities;

public class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // One manager per group. The manager is always also a member
    public int ManagerId { get; set; }
    public User Manager { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Many-to-many with Users, through the GroupMember table
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}
