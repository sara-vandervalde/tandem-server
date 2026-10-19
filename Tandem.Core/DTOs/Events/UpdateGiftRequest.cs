using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Events;

// The gift's responsible member enters what was bought and what they paid (PUT replaces both)
public class UpdateGiftRequest
{
    [StringLength(300, ErrorMessage = "תיאור המתנה ארוך מדי")]
    public string? Description { get; init; }

    // Null = not entered yet; 0 is valid
    [Range(typeof(decimal), "0", "100000", ErrorMessage = "המחיר צריך להיות בין 0 ל-100,000")]
    public decimal? Price { get; init; }
}
