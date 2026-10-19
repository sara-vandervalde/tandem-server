using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    // One query with JOINs for everything the event page shows - no N+1
    public Task<Event?> GetDetailsAsync(int id, CancellationToken cancellationToken) =>
        _context.Events
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.CreatedBy)
            .Include(e => e.Organizer)
            .Include(e => e.Venue)
                .ThenInclude(v => v.Responsible)
            .Include(e => e.Gift)
                .ThenInclude(g => g!.Responsible)
            .Include(e => e.Meal)
                .ThenInclude(m => m.Responsible)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    // Adding the event also inserts its Venue and Meal (set on the navigation properties) in the same SaveChanges
    public void Add(Event evt) => _context.Events.Add(evt);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
