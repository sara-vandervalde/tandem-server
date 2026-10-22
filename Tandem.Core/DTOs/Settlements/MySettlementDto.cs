using Tandem.Core.DTOs.Banks;

namespace Tandem.Core.DTOs.Settlements;

// One line of "my payments": something I pay, or something I receive
public class MySettlementDto
{
    public int Id { get; init; }

    public int EventId { get; init; }

    public string EventTitle { get; init; } = string.Empty;

    public DateTime EventDate { get; init; }

    public int FromUserId { get; init; }

    public string FromUserName { get; init; } = string.Empty;

    public int ToUserId { get; init; }

    public string ToUserName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public bool IsPaid { get; init; }

    public DateTime? PaidAt { get; init; }

    // True when I am the one paying. Set by the service: it depends on who is asking
    public bool IOwe { get; set; }

    // Where to transfer the money. Filled only when I am the payer (and the recipient entered an account)
    public BankAccountDto? RecipientBankAccount { get; set; }
}
