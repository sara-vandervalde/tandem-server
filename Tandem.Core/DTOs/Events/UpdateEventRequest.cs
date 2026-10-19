using System.ComponentModel.DataAnnotations;
using Tandem.Core.Enums;

namespace Tandem.Core.DTOs.Events;

// The organizer's event settings (PUT replaces all of them)
public class UpdateEventRequest
{
    [Required(ErrorMessage = "יש להזין שם לאירוע")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "שם האירוע צריך להכיל בין 2 ל-150 תווים")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "יש לבחור תאריך")]
    public DateTime? EventDate { get; init; }

    [Required(ErrorMessage = "יש לבחור אופן ארגון")]
    public OrganizationMode? OrganizationMode { get; init; }

    public bool HasGift { get; init; }

    [Required(ErrorMessage = "יש לבחור שיטת חלוקה")]
    public SplitMethod? SplitMethod { get; init; }
}
