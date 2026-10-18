using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "יש להזין שם מלא")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "השם צריך להכיל בין 2 ל-100 תווים")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "יש להזין אימייל")]
    [EmailAddress(ErrorMessage = "כתובת האימייל אינה תקינה")]
    [StringLength(256, ErrorMessage = "כתובת האימייל ארוכה מדי")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "יש להזין סיסמה")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "הסיסמה צריכה להכיל לפחות 8 תווים")]
    public string Password { get; init; } = string.Empty;

    [StringLength(200, ErrorMessage = "הכתובת ארוכה מדי")]
    public string? Address { get; init; }
}
