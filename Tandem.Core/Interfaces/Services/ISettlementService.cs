using Tandem.Core.Common;
using Tandem.Core.DTOs.Settlements;

namespace Tandem.Core.Interfaces.Services;

public interface ISettlementService
{
    // Group members and the Admin
    Task<Result<SettlementPreviewDto>> GetPreviewAsync(
        int eventId, int userId, bool isAdmin, CancellationToken cancellationToken);

    // The organizer, only when the event is Organized. Creates the payments and moves the event to Settled
    Task<Result<IReadOnlyList<SettlementDto>>> CloseAsync(int eventId, int userId, CancellationToken cancellationToken);

    // Unpaid first; the recipient's bank account only on the lines I pay
    Task<IReadOnlyList<MySettlementDto>> GetMineAsync(int userId, CancellationToken cancellationToken);

    // Only the payer marks it
    Task<Result> SetPaidAsync(int settlementId, bool isPaid, int userId, CancellationToken cancellationToken);
}
