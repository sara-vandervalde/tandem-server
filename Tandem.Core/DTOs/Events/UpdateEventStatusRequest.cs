using System.ComponentModel.DataAnnotations;
using Tandem.Core.Enums;

namespace Tandem.Core.DTOs.Events;

// Body of PATCH /api/events/{id}. The only status a user sets by hand is Cancelled;
// the others change automatically (Organizing / Organized) or by closing the account (Settled)
public class UpdateEventStatusRequest
{
    [Required(ErrorMessage = "יש לציין סטטוס")]
    [AllowedValues(EventStatus.Cancelled, ErrorMessage = "אפשר רק לבטל אירוע (Cancelled)")]
    public EventStatus? Status { get; init; }
}
