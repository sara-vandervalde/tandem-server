using Microsoft.EntityFrameworkCore;
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

    // Only marks the user for insertion; nothing is written until SaveChangesAsync
    public void Add(User user) => _context.Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
