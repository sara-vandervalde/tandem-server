using System.ComponentModel.DataAnnotations;
using Tandem.Core.DTOs.Common;
using Tandem.Core.Enums;

namespace Tandem.Core.DTOs.Events;

// Query string of GET /api/events: filters, sorting and paging. Every filter is optional
public class EventQuery : PaginationQuery
{
    public int? GroupId { get; init; }

    public EventStatus? Status { get; init; }

    // Part of the title, case-insensitive
    [StringLength(100, ErrorMessage = "טקסט החיפוש ארוך מדי")]
    public string? Search { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    [AllowedValues("date", "title", "status", null, ErrorMessage = "אפשר למיין לפי date, title או status")]
    public string? SortBy { get; init; }

    [AllowedValues("asc", "desc", null, ErrorMessage = "כיוון המיון הוא asc או desc")]
    public string? SortDir { get; init; }
}
