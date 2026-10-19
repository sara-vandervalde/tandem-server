using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

public interface IGroupRepository
{
    // Groups the user is a member of, with manager and members loaded (read-only)
    Task<List<Group>> GetForUserAsync(int userId, CancellationToken cancellationToken);

    // Every group in the system, for the Admin (read-only)
    Task<List<Group>> GetAllAsync(CancellationToken cancellationToken);

    // One group with its manager and each member's user details (read-only)
    Task<Group?> GetDetailsAsync(int id, CancellationToken cancellationToken);

    // Just the group row, for permission checks (read-only)
    Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<bool> IsMemberAsync(int groupId, int userId, CancellationToken cancellationToken);

    // Which of the given emails already belong to members of the group
    Task<List<string>> GetMemberEmailsAsync(
        int groupId, IReadOnlyCollection<string> emails, CancellationToken cancellationToken);

    // Tracked, with the members loaded - for changing the manager or removing a member
    Task<Group?> GetWithMembersForUpdateAsync(int id, CancellationToken cancellationToken);

    // True if the user is responsible for anything in an event of the group that is not settled or cancelled:
    // event organizer, venue / gift / meal, or an item on the "what to bring" list
    Task<bool> HasOpenResponsibilitiesAsync(int groupId, int userId, CancellationToken cancellationToken);

    // Marks the user's rows in the cost-sharing lists of the group's open events for removal
    Task RemoveFromOpenEventParticipantsAsync(int groupId, int userId, CancellationToken cancellationToken);

    void Add(Group group);

    // The database cascades the delete to members, invitations and events (see the Configurations)
    void Remove(Group group);

    // Marks a membership for insertion; saved by the next SaveChangesAsync on the same request
    void AddMember(GroupMember member);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
