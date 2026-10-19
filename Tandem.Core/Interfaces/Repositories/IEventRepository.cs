using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

public interface IEventRepository
{
    // The event page: group, creator, organizer and the responsible member of venue, gift and meal (read-only)
    Task<Event?> GetDetailsAsync(int id, CancellationToken cancellationToken);

    void Add(Event evt);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
