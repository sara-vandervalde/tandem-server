namespace Tandem.Core.DTOs.Invitations;

public class InvitationDto
{
    public int Id { get; init; }

    public int GroupId { get; init; }

    public string GroupName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string InvitedByName { get; init; } = string.Empty;

    // Pending, Accepted, Declined or Cancelled
    public string Status { get; init; } = string.Empty;

    // A pending invitation whose ExpiresAt has passed
    public bool IsExpired { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RespondedAt { get; init; }
}
