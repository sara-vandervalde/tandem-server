using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;

namespace Tandem.Core.Interfaces.Services;

public interface IEventService
{
    // Any member of the group. Creates the event with an empty venue and meal, in one save
    Task<Result<EventDetailsDto>> CreateAsync(CreateEventRequest request, int userId, CancellationToken cancellationToken);

    // Admin: events of every group. Member: events of their groups (403 if they ask for another group)
    Task<Result<PagedResult<EventListItemDto>>> GetEventsAsync(
        EventQuery query, int userId, bool isAdmin, CancellationToken cancellationToken);

    // Members of the group and the Admin; at most one year per request
    Task<Result<IReadOnlyList<EventListItemDto>>> GetCalendarAsync(
        int groupId, CalendarQuery query, int userId, bool isAdmin, CancellationToken cancellationToken);

    // Members of the event's group, and the Admin
    Task<Result<EventDetailsDto>> GetByIdAsync(int eventId, int userId, bool isAdmin, CancellationToken cancellationToken);
}
