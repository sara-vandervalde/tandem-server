namespace Tandem.Core.DTOs.Invitations;

// What happened to each email: invited, or skipped with a reason the manager can read
public class CreateInvitationsResponse
{
    public IReadOnlyList<InvitationDto> Invited { get; init; } = [];

    public IReadOnlyList<SkippedEmailDto> Skipped { get; init; } = [];
}
