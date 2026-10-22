namespace Tandem.Core.DTOs.Settlements;

// One line of the live estimate: who would pay whom, if the account were closed now
public class TransferDto
{
    public int FromUserId { get; init; }

    public string FromUserName { get; init; } = string.Empty;

    public int ToUserId { get; init; }

    public string ToUserName { get; init; } = string.Empty;

    public decimal Amount { get; init; }
}
