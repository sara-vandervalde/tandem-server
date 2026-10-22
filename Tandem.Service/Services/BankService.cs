using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Banks;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;

namespace Tandem.Service.Services;

public class BankService : IBankService
{
    private readonly IBankRepository _bankRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<BankService> _logger;

    public BankService(IBankRepository bankRepository, IMapper mapper, ILogger<BankService> logger)
    {
        _bankRepository = bankRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BankDto>> GetBanksAsync(CancellationToken cancellationToken)
    {
        var banks = await _bankRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<BankDto>>(banks);
    }

    public async Task<Result<BankAccountDto>> GetMyAccountAsync(int userId, CancellationToken cancellationToken)
    {
        var account = await _bankRepository.GetAccountAsync(userId, cancellationToken);

        return account is null
            ? Result<BankAccountDto>.NotFound("עוד לא הוזנו פרטי חשבון בנק")
            : Result<BankAccountDto>.Success(_mapper.Map<BankAccountDto>(account));
    }

    public async Task<Result> SaveMyAccountAsync(
        int userId, SaveBankAccountRequest request, CancellationToken cancellationToken)
    {
        var bankId = request.BankId!.Value;
        if (!await _bankRepository.BankExistsAsync(bankId, cancellationToken))
        {
            return Result.ValidationError("הבנק שנבחר לא קיים ברשימה");
        }

        // PUT = "this is my account": the first time it is created, afterwards it is replaced
        var account = await _bankRepository.GetAccountForUpdateAsync(userId, cancellationToken);
        if (account is null)
        {
            account = new BankAccount { UserId = userId };
            _bankRepository.AddAccount(account);
        }

        account.BankId = bankId;
        account.BranchNumber = request.BranchNumber;
        account.AccountNumber = request.AccountNumber;
        account.AccountHolderName = request.AccountHolderName.Trim();

        await _bankRepository.SaveChangesAsync(cancellationToken);

        // The account details themselves are never logged
        _logger.LogInformation("User {UserId} saved their bank account", userId);
        return Result.Success();
    }
}
