using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

public interface IInvitationRepository
{
    // Pending invitations (expired or not) of a group for the given emails - tracked, so they can be renewed
    Task<List<GroupInvitation>> GetPendingForEmailsAsync(
        int groupId, IReadOnlyCollection<string> emails, CancellationToken cancellationToken);

    // All invitations of a group, newest first, with the group and inviter loaded (read-only)
    Task<List<GroupInvitation>> GetForGroupAsync(int groupId, CancellationToken cancellationToken);

    void AddRange(IEnumerable<GroupInvitation> invitations);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
