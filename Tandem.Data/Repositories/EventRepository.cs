using Microsoft.EntityFrameworkCore;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
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
            .Include(e => e.Participants)
                .ThenInclude(p => p.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<PagedResult<Event>> GetPagedAsync(
        EventQuery query, int? visibleToUserId, CancellationToken cancellationToken)
    {
        // The query is built step by step and runs once, in the database: WHERE, ORDER BY, LIMIT/OFFSET
        var events = _context.Events.AsNoTracking();

        if (visibleToUserId is int userId)
        {
            events = events.Where(e => e.Group.Members.Any(m => m.UserId == userId));
        }
        if (query.GroupId is int groupId)
        {
            events = events.Where(e => e.GroupId == groupId);
        }
        if (query.Status is EventStatus status)
        {
            events = events.Where(e => e.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ILIKE: PostgreSQL's case-insensitive LIKE
            var pattern = $"%{query.Search.Trim()}%";
            events = events.Where(e => EF.Functions.ILike(e.Title, pattern));
        }
        if (query.From is DateTime from)
        {
            events = events.Where(e => e.EventDate >= from);
        }
        if (query.To is DateTime to)
        {
            events = events.Where(e => e.EventDate <= to);
        }

        var totalCount = await events.CountAsync(cancellationToken);

        var descending = query.SortDir == "desc";
        var sorted = query.SortBy switch
        {
            "title" => descending ? events.OrderByDescending(e => e.Title) : events.OrderBy(e => e.Title),
            "status" => descending ? events.OrderByDescending(e => e.Status) : events.OrderBy(e => e.Status),
            _ => descending ? events.OrderByDescending(e => e.EventDate) : events.OrderBy(e => e.EventDate)
        };

        var items = await sorted
            .ThenBy(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(e => e.Group)
            .Include(e => e.Organizer)
            .ToListAsync(cancellationToken);

        return new PagedResult<Event>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<List<Event>> GetForGroupInRangeAsync(
        int groupId, DateTime from, DateTime to, CancellationToken cancellationToken) =>
        _context.Events
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Organizer)
            .Where(e => e.GroupId == groupId && e.EventDate >= from && e.EventDate <= to)
            .OrderBy(e => e.EventDate)
            .ToListAsync(cancellationToken);

    public Task<Event?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _context.Events
            .Include(e => e.Group)
            .Include(e => e.Gift)
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Remove(Event evt) => _context.Events.Remove(evt);

    // Adding the event also inserts its Venue and Meal (set on the navigation properties) in the same SaveChanges
    public void Add(Event evt) => _context.Events.Add(evt);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
