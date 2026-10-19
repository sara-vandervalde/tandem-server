namespace Tandem.Core.DTOs.Events;

// An event in a list or on the calendar
public class EventListItemDto
{
    public int Id { get; init; }

    public int GroupId { get; init; }

    public string GroupName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateTime EventDate { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? OrganizerName { get; init; }

    public bool HasGift { get; init; }
}
