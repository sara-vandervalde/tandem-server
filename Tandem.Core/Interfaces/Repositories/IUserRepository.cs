using Tandem.Core.Common;
using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

// Data access for users. Emails are expected in lowercase - the service normalizes them
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    // Read-only
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // Tracked, so changes to the returned user are saved by SaveChangesAsync
    Task<User?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
