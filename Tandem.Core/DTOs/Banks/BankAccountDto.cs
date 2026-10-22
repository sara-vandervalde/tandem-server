namespace Tandem.Core.DTOs.Banks;

// A member's bank account. Returned only to its owner, and later to a member who owes them money
public class BankAccountDto
{
    public int BankId { get; init; }

    public string BankName { get; init; } = string.Empty;

    public string BranchNumber { get; init; } = string.Empty;

    public string AccountNumber { get; init; } = string.Empty;

    public string AccountHolderName { get; init; } = string.Empty;
}
