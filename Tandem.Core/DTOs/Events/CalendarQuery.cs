using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Events;

// Query string of the group calendar: ?from=2026-11-01&to=2026-11-30
public class CalendarQuery
{
    [Required(ErrorMessage = "יש לציין תאריך התחלה")]
    public DateTime? From { get; init; }

    [Required(ErrorMessage = "יש לציין תאריך סיום")]
    public DateTime? To { get; init; }
}
