using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces.Repositories;

// The bank list and the members' bank accounts
public interface IBankRepository
{
    // Read-only, ordered by name
    Task<List<Bank>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> BankExistsAsync(int bankId, CancellationToken cancellationToken);

    // Read-only, with the bank's name
    Task<BankAccount?> GetAccountAsync(int userId, CancellationToken cancellationToken);

    // Read-only, several users in one query (no loop) - for "my payments"
    Task<List<BankAccount>> GetAccountsAsync(IReadOnlyCollection<int> userIds, CancellationToken cancellationToken);

    // Tracked, so changes are saved by SaveChangesAsync
    Task<BankAccount?> GetAccountForUpdateAsync(int userId, CancellationToken cancellationToken);

    void AddAccount(BankAccount account);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
