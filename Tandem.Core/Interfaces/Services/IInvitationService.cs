using Tandem.Core.Common;
using Tandem.Core.DTOs.Invitations;

namespace Tandem.Core.Interfaces.Services;

public interface IInvitationService
{
    // Manager only. Skips members, invalid emails and emails that already have a pending invitation
    Task<Result<CreateInvitationsResponse>> CreateAsync(
        int groupId, CreateInvitationsRequest request, int userId, CancellationToken cancellationToken);

    // Manager only: every invitation of the group and its status
    Task<Result<IReadOnlyList<InvitationDto>>> GetForGroupAsync(
        int groupId, int userId, CancellationToken cancellationToken);

    // The logged-in user's pending, unexpired invitations, matched by email (from any group)
    Task<IReadOnlyList<InvitationDto>> GetMyInvitationsAsync(string userEmail, CancellationToken cancellationToken);

    // Only the invited user. Accepting adds them to the group in the same save. Already handled or expired: 409
    Task<Result> RespondAsync(
        int invitationId, bool accept, int userId, string userEmail, CancellationToken cancellationToken);

    // Manager only, and only while the invitation is pending
    Task<Result> CancelAsync(int invitationId, int userId, CancellationToken cancellationToken);
}
