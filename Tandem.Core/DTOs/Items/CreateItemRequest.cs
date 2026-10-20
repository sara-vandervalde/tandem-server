using System.ComponentModel.DataAnnotations;
using Tandem.Core.Enums;

namespace Tandem.Core.DTOs.Items;

// Food is added by the meal's responsible member, equipment by the venue's
public class CreateItemRequest
{
    [Required(ErrorMessage = "יש לבחור סוג פריט")]
    public ItemType? Type { get; init; }

    [Required(ErrorMessage = "יש להזין שם לפריט")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "שם הפריט צריך להכיל בין 2 ל-100 תווים")]
    public string Name { get; init; } = string.Empty;
}
