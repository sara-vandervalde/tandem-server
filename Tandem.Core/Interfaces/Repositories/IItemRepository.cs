using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

public interface IItemRepository
{
    // The event's list, read-only. Private items are filtered in the query itself:
    // they are returned only when userId is the meal's responsible member
    Task<List<EventItem>> GetForEventAsync(int eventId, int userId, CancellationToken cancellationToken);

    // One item with who added it and who brings it (read-only) - the response after creating
    Task<EventItem?> GetDetailsAsync(int id, CancellationToken cancellationToken);

    // Tracked, with the event, its venue and its meal - for the permission checks before a change
    Task<EventItem?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    void Add(EventItem item);

    void Remove(EventItem item);

    // Throws ConcurrencyConflictException when the item changed since it was loaded
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
