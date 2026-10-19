using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Data.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly AppDbContext _context;

    public GroupRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Group>> GetForUserAsync(int userId, CancellationToken cancellationToken) =>
        WithManagerAndMembers()
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    public Task<List<Group>> GetAllAsync(CancellationToken cancellationToken) =>
        WithManagerAndMembers()
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    // Include + ThenInclude load the group, its manager, and every member's user in one query - no N+1
    public Task<Group?> GetDetailsAsync(int id, CancellationToken cancellationToken) =>
        _context.Groups
            .AsNoTracking()
            .Include(g => g.Manager)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<bool> IsMemberAsync(int groupId, int userId, CancellationToken cancellationToken) =>
        _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

    public Task<List<int>> GetMemberIdsAsync(int groupId, CancellationToken cancellationToken) =>
        _context.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

    public Task<List<string>> GetMemberEmailsAsync(
        int groupId, IReadOnlyCollection<string> emails, CancellationToken cancellationToken) =>
        _context.GroupMembers
            .Where(m => m.GroupId == groupId && emails.Contains(m.User.Email))
            .Select(m => m.User.Email)
            .ToListAsync(cancellationToken);

    public Task<Group?> GetWithMembersForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    // One EXISTS query per kind of responsibility, all answered by the database
    public async Task<bool> HasOpenResponsibilitiesAsync(int groupId, int userId, CancellationToken cancellationToken)
    {
        var openEvents = OpenEventsOf(groupId);

        return await openEvents.AnyAsync(e => e.OrganizerId == userId, cancellationToken)
               || await _context.Venues.AnyAsync(v => v.ResponsibleId == userId && openEvents.Contains(v.Event), cancellationToken)
               || await _context.Gifts.AnyAsync(g => g.ResponsibleId == userId && openEvents.Contains(g.Event), cancellationToken)
               || await _context.Meals.AnyAsync(m => m.ResponsibleId == userId && openEvents.Contains(m.Event), cancellationToken)
               || await _context.EventItems.AnyAsync(i => i.AssigneeId == userId && openEvents.Contains(i.Event), cancellationToken);
    }

    public async Task RemoveFromOpenEventParticipantsAsync(int groupId, int userId, CancellationToken cancellationToken)
    {
        var openEvents = OpenEventsOf(groupId);
        var participations = await _context.EventParticipants
            .Where(p => p.UserId == userId && openEvents.Contains(p.Event))
            .ToListAsync(cancellationToken);

        _context.EventParticipants.RemoveRange(participations);
    }

    // Adding the group also inserts any GroupMember rows in group.Members, in the same SaveChanges
    public void Add(Group group) => _context.Groups.Add(group);

    public void Remove(Group group) => _context.Groups.Remove(group);

    public void AddMember(GroupMember member) => _context.GroupMembers.Add(member);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    // Events whose costs are not settled yet - a member's responsibilities there still matter
    private IQueryable<Event> OpenEventsOf(int groupId) =>
        _context.Events.Where(e => e.GroupId == groupId
                                   && e.Status != EventStatus.Settled
                                   && e.Status != EventStatus.Cancelled);

    private IQueryable<Group> WithManagerAndMembers() =>
        _context.Groups
            .AsNoTracking()
            .Include(g => g.Manager)
            .Include(g => g.Members);
}
