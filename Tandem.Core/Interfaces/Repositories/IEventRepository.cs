using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;
using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

public interface IEventRepository
{
    // The event page: group, creator, organizer and the responsible member of venue, gift and meal (read-only)
    Task<Event?> GetDetailsAsync(int id, CancellationToken cancellationToken);

    // Filtered, sorted page of events. visibleToUserId = null means every group (Admin);
    // otherwise only events of groups the user belongs to. Dates are expected in UTC
    Task<PagedResult<Event>> GetPagedAsync(EventQuery query, int? visibleToUserId, CancellationToken cancellationToken);

    // The group calendar: events between two dates, by date (read-only)
    Task<List<Event>> GetForGroupInRangeAsync(int groupId, DateTime from, DateTime to, CancellationToken cancellationToken);

    // Tracked, with the group, gift and participants - for editing, cancelling or deleting
    Task<Event?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    void Add(Event evt);

    // The database cascades the delete to venue, gift, meal, items and participants
    void Remove(Event evt);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
