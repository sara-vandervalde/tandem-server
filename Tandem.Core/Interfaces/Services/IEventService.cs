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

    // At most one year per request. Group access is checked by the caller ([RequireGroupMember])
    Task<Result<IReadOnlyList<EventListItemDto>>> GetCalendarAsync(
        int groupId, CalendarQuery query, CancellationToken cancellationToken);

    // Organizer only, while the event is not settled or cancelled
    Task<Result> UpdateAsync(int eventId, UpdateEventRequest request, int userId, CancellationToken cancellationToken);

    // Organizer only, and only with SplitMethod = SelectedMembers. Everyone chosen must be a group member
    Task<Result> SetParticipantsAsync(
        int eventId, SetParticipantsRequest request, int userId, CancellationToken cancellationToken);

    // Organizer only; a settled event cannot be cancelled
    Task<Result> CancelAsync(int eventId, int userId, CancellationToken cancellationToken);

    // Draft: its creator or the group manager. With an organizer: the organizer only
    Task<Result> DeleteAsync(int eventId, int userId, CancellationToken cancellationToken);

    // Members of the event's group, and the Admin
    Task<Result<EventDetailsDto>> GetByIdAsync(int eventId, int userId, bool isAdmin, CancellationToken cancellationToken);
}
