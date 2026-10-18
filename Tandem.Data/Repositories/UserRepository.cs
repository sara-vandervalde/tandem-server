using Microsoft.EntityFrameworkCore;
using Tandem.Core.Common;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    // Read-only: AsNoTracking, because the user is only checked, not changed
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    // Skip/Take run inside the SQL query (LIMIT/OFFSET), so only one page is loaded from the database
    public async Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    // Only marks the user for insertion; nothing is written until SaveChangesAsync
    public void Add(User user) => _context.Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
