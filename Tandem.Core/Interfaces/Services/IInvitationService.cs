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
}
