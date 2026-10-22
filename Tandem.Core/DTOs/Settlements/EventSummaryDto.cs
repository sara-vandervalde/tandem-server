using Tandem.Core.DTOs.Events;
using Tandem.Core.DTOs.Items;

namespace Tandem.Core.DTOs.Settlements;

// GET /api/events/{id}/summary: everything about an event on one page (also for printing).
// Built from the DTOs that already exist, so the event page and the summary always show the same data
public class EventSummaryDto
{
    // Date, organizer, venue, meal and gift with their responsible members and prices
    public EventDetailsDto Event { get; init; } = new();

    // The "what to bring" list and who brings each item (the cook's private menu only for the cook)
    public IReadOnlyList<ItemDto> Items { get; init; } = [];

    // True after the account was closed: Settlements holds the final payments
    public bool IsFinal { get; init; }

    // Before closing: the live estimate. Null after closing
    public SettlementPreviewDto? Preview { get; init; }

    // After closing: the saved payments, with "paid" marks. Empty before closing
    public IReadOnlyList<SettlementDto> Settlements { get; init; } = [];
}
