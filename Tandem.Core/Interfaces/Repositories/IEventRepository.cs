using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;
using Tandem.Core.Entities;
using Tandem.Core.Enums;

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

    // Tracked, with the group, venue, gift, meal and participants - for editing, claiming, cancelling or deleting
    Task<Event?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    // Only the event's group, for a permission check (null = no such event)
    Task<int?> GetGroupIdAsync(int eventId, CancellationToken cancellationToken);

    // Whether the event's "what to bring" list has items of this type
    Task<bool> HasItemsAsync(int eventId, ItemType type, CancellationToken cancellationToken);

    // Tracked items of one type - when a meal type change turns the food items private or public
    Task<List<EventItem>> GetItemsForUpdateAsync(int eventId, ItemType type, CancellationToken cancellationToken);

    void Add(Event evt);

    // The database cascades the delete to venue, gift, meal, items and participants
    void Remove(Event evt);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
