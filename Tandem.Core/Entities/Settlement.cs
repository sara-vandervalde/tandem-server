namespace Tandem.Core.Entities;

// One payment created when the organizer closes the event's account: FromUser transfers Amount to ToUser.
// The money moves outside the system; here the payer only marks "I paid"
public class Settlement
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int FromUserId { get; set; }
    public User FromUser { get; set; } = null!;

    public int ToUserId { get; set; }
    public User ToUser { get; set; } = null!;

    public decimal Amount { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidAt { get; set; }
}
