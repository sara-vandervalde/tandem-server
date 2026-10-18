namespace Tandem.Core.Entities;

// Created together with its event, with no responsible member.
// Claiming it is an update of ResponsibleId, protected by Version
public class Venue
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int? ResponsibleId { get; set; }
    public User? Responsible { get; set; }

    public string? Address { get; set; }

    // Null = not entered yet; 0 = free
    public decimal? Price { get; set; }

    public uint Version { get; set; }
}
