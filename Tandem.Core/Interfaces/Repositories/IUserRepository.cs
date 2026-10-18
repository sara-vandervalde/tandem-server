using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

// Data access for users. Emails are expected in lowercase - the service normalizes them
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
