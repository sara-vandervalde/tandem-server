using Microsoft.EntityFrameworkCore;
using Tandem.Core.Common;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Data.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<EventMessage>> GetPagedAsync(
        int eventId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var messages = _context.EventMessages
            .AsNoTracking()
            .Where(m => m.EventId == eventId);

        var totalCount = await messages.CountAsync(cancellationToken);

        // Skip/Take inside the query: the database returns only one page
        var items = await messages
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.User)
            .ToListAsync(cancellationToken);

        return new PagedResult<EventMessage>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task<EventMessage?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.EventMessages
            .AsNoTracking()
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public void Add(EventMessage message) => _context.EventMessages.Add(message);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
