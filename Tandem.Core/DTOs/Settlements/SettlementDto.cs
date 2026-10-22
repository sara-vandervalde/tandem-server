namespace Tandem.Core.DTOs.Settlements;

// A saved payment, created when the account is closed
public class SettlementDto
{
    public int Id { get; init; }

    public int EventId { get; init; }

    public int FromUserId { get; init; }

    public string FromUserName { get; init; } = string.Empty;

    public int ToUserId { get; init; }

    public string ToUserName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public bool IsPaid { get; init; }

    public DateTime? PaidAt { get; init; }
}
