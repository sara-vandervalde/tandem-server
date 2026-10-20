using System.ComponentModel.DataAnnotations;
using Tandem.Core.Enums;

namespace Tandem.Core.DTOs.Events;

// The meal's responsible member chooses the meal type and enters what they paid (PUT replaces all)
public class UpdateMealRequest
{
    // Null = not chosen yet
    public MealType? MealType { get; init; }

    // Kept only when MealType is Catering
    [StringLength(150, ErrorMessage = "שם הקייטרינג ארוך מדי")]
    public string? CateringName { get; init; }

    // Catering only: "the caterer brings tableware" - then no equipment items can be added
    public bool CateringIncludesTableware { get; init; }

    // Null = not entered yet; 0 is valid (e.g. a shared meal where everyone brings a dish)
    [Range(typeof(decimal), "0", "100000", ErrorMessage = "המחיר צריך להיות בין 0 ל-100,000")]
    public decimal? Price { get; init; }
}
