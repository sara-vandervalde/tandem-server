namespace Tandem.Core.Entities;

// Where a member receives money from others. One per user (unique index on UserId in the Configuration).
// Sensitive: shown only to a member who owes the account holder money, and never written to the log
public class BankAccount
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BankId { get; set; }
    public Bank Bank { get; set; } = null!;

    // Text, not numbers: a branch or account number may start with 0
    public string BranchNumber { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string AccountHolderName { get; set; } = string.Empty;
}
