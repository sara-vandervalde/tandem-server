namespace Tandem.Core.DTOs.Events;

public class MealDto
{
    public int? ResponsibleId { get; init; }

    public string? ResponsibleName { get; init; }

    // Shared, Catering or SelfMade; null until the responsible member chooses
    public string? MealType { get; init; }

    public string? CateringName { get; init; }

    public decimal? Price { get; init; }
}
