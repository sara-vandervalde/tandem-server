using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Events;

public class CreateEventRequest
{
    [Required(ErrorMessage = "יש לבחור קבוצה")]
    [Range(1, int.MaxValue, ErrorMessage = "מזהה קבוצה לא תקין")]
    public int? GroupId { get; init; }

    [Required(ErrorMessage = "יש להזין שם לאירוע")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "שם האירוע צריך להכיל בין 2 ל-150 תווים")]
    public string Title { get; init; } = string.Empty;

    // Sent in UTC (ISO 8601 with "Z"); the service rejects dates in the past
    [Required(ErrorMessage = "יש לבחור תאריך")]
    public DateTime? EventDate { get; init; }

    // The answer to "are you the organizer?" asked right after creating. No = the event stays a draft
    public bool IsOrganizer { get; init; }
}
