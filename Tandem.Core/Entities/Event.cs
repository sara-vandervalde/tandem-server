using Tandem.Core.Enums;

namespace Tandem.Core.Entities;

public class Event
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    // Who added the event to the calendar. Not necessarily the organizer
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    // Null while the event is a draft. The first member to claim it becomes the organizer
    public int? OrganizerId { get; set; }
    public User? Organizer { get; set; }

    public OrganizationMode OrganizationMode { get; set; } = OrganizationMode.Open;

    public bool HasGift { get; set; }

    public SplitMethod SplitMethod { get; set; } = SplitMethod.Equal;

    public EventStatus Status { get; set; } = EventStatus.Draft;

    // Concurrency token, mapped to PostgreSQL's xmin column in the Configuration
    public uint Version { get; set; }

    public Venue Venue { get; set; } = null!;

    // Null when HasGift is false
    public Gift? Gift { get; set; }

    public Meal Meal { get; set; } = null!;

    // Used only when SplitMethod is SelectedMembers
    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();

    public ICollection<EventItem> Items { get; set; } = new List<EventItem>();
}
