namespace Tandem.Core.DTOs.Auth;

// Returned after a successful login. The client stores the token and sends it with every request
public class AuthResponse
{
    public string Token { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }

    public int UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
