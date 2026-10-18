namespace Tandem.Core.Entities;

// Join table between Events and Users: who shares the cost when SplitMethod is SelectedMembers.
// The key is (EventId, UserId) - defined in the Configuration
public class EventParticipant
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
