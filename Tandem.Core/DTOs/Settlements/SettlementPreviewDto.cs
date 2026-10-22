namespace Tandem.Core.DTOs.Settlements;

// GET /api/events/{id}/settlement-preview: calculated on every request from the current prices, never saved
public class SettlementPreviewDto
{
    // Venue + gift + meal, only the prices entered so far
    public decimal Total { get; init; }

    // How many members share the cost (depends on the split method)
    public int SharerCount { get; init; }

    public IReadOnlyList<TransferDto> Transfers { get; init; } = [];
}
