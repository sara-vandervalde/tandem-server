using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Core.Exceptions;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Data.Repositories;

public class SettlementRepository : ISettlementRepository
{
    private readonly AppDbContext _context;

    public SettlementRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Event?> GetEventForPreviewAsync(int eventId, CancellationToken cancellationToken) =>
        EventForSettlement()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

    public Task<Event?> GetEventForCloseAsync(int eventId, CancellationToken cancellationToken) =>
        EventForSettlement()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

    public Task<List<Settlement>> GetForUserAsync(int userId, CancellationToken cancellationToken) =>
        _context.Settlements
            .AsNoTracking()
            .Include(s => s.Event)
            .Include(s => s.FromUser)
            .Include(s => s.ToUser)
            .Where(s => s.FromUserId == userId || s.ToUserId == userId)
            .OrderBy(s => s.IsPaid)
            .ThenByDescending(s => s.Event.EventDate)
            .ToListAsync(cancellationToken);

    public Task<Settlement?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _context.Settlements.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void AddRange(IEnumerable<Settlement> settlements) => _context.Settlements.AddRange(settlements);

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

    // One query (split into a few SELECTs) instead of a loop that loads each member: no N+1
    private IQueryable<Event> EventForSettlement() =>
        _context.Events
            .Include(e => e.Venue)
            .Include(e => e.Gift)
            .Include(e => e.Meal)
            .Include(e => e.Participants)
                .ThenInclude(p => p.User)
            .Include(e => e.Group)
                .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.User)
            .AsSplitQuery();
}
