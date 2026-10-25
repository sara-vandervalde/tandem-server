namespace Tandem.Core.Entities;

// A note on the event's message board ("maybe add a sugar-free cake?").
// Not a live chat: the page refreshes every few seconds
public class EventMessage
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
