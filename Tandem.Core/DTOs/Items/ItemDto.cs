namespace Tandem.Core.DTOs.Items;

// One line in the "what to bring" list
public class ItemDto
{
    public int Id { get; init; }

    public int EventId { get; init; }

    // "Food" or "Equipment"
    public string Type { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    // True only in a SelfMade meal's private menu (seen only by the meal's responsible member)
    public bool IsPrivate { get; init; }

    public int AddedById { get; init; }

    public string AddedByName { get; init; } = string.Empty;

    // Null = nobody is bringing it yet
    public int? AssigneeId { get; init; }

    public string? AssigneeName { get; init; }
}
