using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Data.Repositories;

public class BankRepository : IBankRepository
{
    private readonly AppDbContext _context;

    public BankRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Bank>> GetAllAsync(CancellationToken cancellationToken) =>
        _context.Banks
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> BankExistsAsync(int bankId, CancellationToken cancellationToken) =>
        _context.Banks.AnyAsync(b => b.Id == bankId, cancellationToken);

    public Task<BankAccount?> GetAccountAsync(int userId, CancellationToken cancellationToken) =>
        _context.BankAccounts
            .AsNoTracking()
            .Include(a => a.Bank)
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

    // WHERE "UserId" IN (...): one query for all the recipients
    public Task<List<BankAccount>> GetAccountsAsync(IReadOnlyCollection<int> userIds, CancellationToken cancellationToken) =>
        _context.BankAccounts
            .AsNoTracking()
            .Include(a => a.Bank)
            .Where(a => userIds.Contains(a.UserId))
            .ToListAsync(cancellationToken);

    public Task<BankAccount?> GetAccountForUpdateAsync(int userId, CancellationToken cancellationToken) =>
        _context.BankAccounts.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

    public void AddAccount(BankAccount account) => _context.BankAccounts.Add(account);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
