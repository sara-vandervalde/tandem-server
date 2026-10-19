namespace Tandem.Core.DTOs.Events;

// The event page: the event, its organizer and the three areas of responsibility
public class EventDetailsDto
{
    public int Id { get; init; }

    public int GroupId { get; init; }

    public string GroupName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateTime EventDate { get; init; }

    public int CreatedById { get; init; }

    public string CreatedByName { get; init; } = string.Empty;

    public int? OrganizerId { get; init; }

    public string? OrganizerName { get; init; }

    public string OrganizationMode { get; init; } = string.Empty;

    public bool HasGift { get; init; }

    public string SplitMethod { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public VenueDto Venue { get; init; } = new();

    // Null when the event has no gift
    public GiftDto? Gift { get; init; }

    public MealDto Meal { get; init; } = new();
}
