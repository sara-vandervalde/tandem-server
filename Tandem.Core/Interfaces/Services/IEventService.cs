using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;

namespace Tandem.Core.Interfaces.Services;

public interface IEventService
{
    // Any member of the group. Creates the event with an empty venue and meal, in one save
    Task<Result<EventDetailsDto>> CreateAsync(CreateEventRequest request, int userId, CancellationToken cancellationToken);

    // Members of the event's group, and the Admin
    Task<Result<EventDetailsDto>> GetByIdAsync(int eventId, int userId, bool isAdmin, CancellationToken cancellationToken);
}
