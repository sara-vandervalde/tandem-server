using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Core.Exceptions;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Data.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _context;

    public ItemRepository(AppDbContext context)
    {
        _context = context;
    }

    // The privacy filter runs in SQL, so a private item never even leaves the database for anyone else
    public Task<List<EventItem>> GetForEventAsync(int eventId, int userId, CancellationToken cancellationToken) =>
        _context.EventItems
            .AsNoTracking()
            .Include(i => i.AddedBy)
            .Include(i => i.Assignee)
            .Where(i => i.EventId == eventId)
            .Where(i => !i.IsPrivate || i.Event.Meal.ResponsibleId == userId)
            .OrderBy(i => i.Type)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

    public Task<EventItem?> GetDetailsAsync(int id, CancellationToken cancellationToken) =>
        _context.EventItems
            .AsNoTracking()
            .Include(i => i.AddedBy)
            .Include(i => i.Assignee)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<EventItem?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _context.EventItems
            .Include(i => i.Event)
                .ThenInclude(e => e.Venue)
            .Include(i => i.Event)
                .ThenInclude(e => e.Meal)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public void Add(EventItem item) => _context.EventItems.Add(item);

    public void Remove(EventItem item) => _context.EventItems.Remove(item);

    // Same translation as in EventRepository: the service knows Core's exception, never EF Core's
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }
    }
}
