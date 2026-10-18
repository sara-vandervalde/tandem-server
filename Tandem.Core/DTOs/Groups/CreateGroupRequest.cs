using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Groups;

public class CreateGroupRequest
{
    [Required(ErrorMessage = "יש להזין שם לקבוצה")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "שם הקבוצה צריך להכיל בין 2 ל-100 תווים")]
    public string Name { get; init; } = string.Empty;
}
