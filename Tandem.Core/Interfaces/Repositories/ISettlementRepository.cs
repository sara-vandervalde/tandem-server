using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

public interface ISettlementRepository
{
    // Everything the calculation needs, in one query: venue, gift, meal, participants and group members (with names).
    // Read-only - for the live estimate
    Task<Event?> GetEventForPreviewAsync(int eventId, CancellationToken cancellationToken);

    // Same, tracked - for closing the account (the status change is protected by the event's version)
    Task<Event?> GetEventForCloseAsync(int eventId, CancellationToken cancellationToken);

    // Read-only: every payment the user pays or receives, with the event and both names
    Task<List<Settlement>> GetForUserAsync(int userId, CancellationToken cancellationToken);

    // Read-only: the event's final payments, with both names - for the summary
    Task<List<Settlement>> GetForEventAsync(int eventId, CancellationToken cancellationToken);

    // Tracked - for marking "I paid"
    Task<Settlement?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    void AddRange(IEnumerable<Settlement> settlements);

    // Throws ConcurrencyConflictException when the event changed since it was loaded
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
