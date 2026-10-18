using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
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

    // Adding the group also inserts any GroupMember rows in group.Members, in the same SaveChanges
    public void Add(Group group) => _context.Groups.Add(group);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    private IQueryable<Group> WithManagerAndMembers() =>
        _context.Groups
            .AsNoTracking()
            .Include(g => g.Manager)
            .Include(g => g.Members);
}
