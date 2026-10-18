using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "יש להזין אימייל")]
    [EmailAddress(ErrorMessage = "כתובת האימייל אינה תקינה")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "יש להזין סיסמה")]
    public string Password { get; init; } = string.Empty;
}
