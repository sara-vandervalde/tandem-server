using Tandem.Core.Common;
using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

// The event's message board
public interface IMessageRepository
{
    // Read-only, newest first, one page, with the writer's name
    Task<PagedResult<EventMessage>> GetPagedAsync(int eventId, int page, int pageSize, CancellationToken cancellationToken);

    // Read-only, with the writer's name - to return the new message after saving
    Task<EventMessage?> GetByIdAsync(int id, CancellationToken cancellationToken);

    void Add(EventMessage message);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
