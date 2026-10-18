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

    void Add(Group group);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
