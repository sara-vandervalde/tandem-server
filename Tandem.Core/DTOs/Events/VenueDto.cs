namespace Tandem.Core.DTOs.Events;

public class VenueDto
{
    public int? ResponsibleId { get; init; }

    public string? ResponsibleName { get; init; }

    public string? Address { get; init; }

    // Null = not entered yet; 0 = free
    public decimal? Price { get; init; }
}
