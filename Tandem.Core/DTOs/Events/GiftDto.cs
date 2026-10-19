namespace Tandem.Core.DTOs.Events;

public class GiftDto
{
    public int? ResponsibleId { get; init; }

    public string? ResponsibleName { get; init; }

    public string? Description { get; init; }

    public decimal? Price { get; init; }
}
