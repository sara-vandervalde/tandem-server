using Tandem.Core.Common;
using Tandem.Core.DTOs.Banks;

namespace Tandem.Core.Interfaces.Services;

public interface IBankService
{
    Task<IReadOnlyList<BankDto>> GetBanksAsync(CancellationToken cancellationToken);

    // 404 while the user has not entered an account yet
    Task<Result<BankAccountDto>> GetMyAccountAsync(int userId, CancellationToken cancellationToken);

    // Creates or replaces; 400 if the bank does not exist
    Task<Result> SaveMyAccountAsync(int userId, SaveBankAccountRequest request, CancellationToken cancellationToken);
}
