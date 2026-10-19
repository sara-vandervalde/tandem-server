using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Events;

// The venue's responsible member enters the address and what they paid (PUT replaces both)
public class UpdateVenueRequest
{
    [StringLength(200, ErrorMessage = "הכתובת ארוכה מדי")]
    public string? Address { get; init; }

    // Null = not entered yet; 0 is valid (e.g. a member's home)
    [Range(typeof(decimal), "0", "100000", ErrorMessage = "המחיר צריך להיות בין 0 ל-100,000")]
    public decimal? Price { get; init; }
}
