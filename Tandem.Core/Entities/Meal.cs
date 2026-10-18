using Tandem.Core.Enums;

namespace Tandem.Core.Entities;

// Created together with its event, with no responsible member
public class Meal
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int? ResponsibleId { get; set; }
    public User? Responsible { get; set; }

    // Null until the responsible member chooses
    public MealType? MealType { get; set; }

    // Relevant only when MealType is Catering
    public string? CateringName { get; set; }

    // Null = not entered yet; 0 = free
    public decimal? Price { get; set; }

    public uint Version { get; set; }
}
