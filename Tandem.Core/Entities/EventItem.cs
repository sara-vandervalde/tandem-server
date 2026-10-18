using Tandem.Core.Enums;

namespace Tandem.Core.Entities;

// One item in the event's "what to bring" list. Members bring items at their own expense, so there is no price
public class EventItem
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public ItemType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    // True only for food items of a SelfMade meal: visible to the meal's responsible member only
    public bool IsPrivate { get; set; }

    // Meal responsible for food, venue responsible for equipment
    public int AddedById { get; set; }
    public User AddedBy { get; set; } = null!;

    // Null = nobody is bringing it yet
    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public uint Version { get; set; }
}
