using Tandem.Core.Enums;

namespace Tandem.Core.Entities;

// Linked to an email, not to a user - so people who have not registered yet can be invited
public class GroupInvitation
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    // Stored in lowercase, to match the user's email regardless of letter case
    public string Email { get; set; } = string.Empty;

    public int InvitedById { get; set; }
    public User InvitedBy { get; set; } = null!;

    // An expired invitation stays Pending; queries compare ExpiresAt with the current time
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Set by the service when the invitation is created
    public DateTime ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
