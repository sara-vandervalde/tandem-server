namespace Tandem.Core.Entities;

// Exists only when the event has HasGift = true
public class Gift : IResponsibility
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int? ResponsibleId { get; set; }
    public User? Responsible { get; set; }

    public string? Description { get; set; }

    // Null = not entered yet; 0 = free
    public decimal? Price { get; set; }

    public uint Version { get; set; }
}
