namespace Tandem.Core.DTOs.Users;

// What the API exposes about a user. Never includes the password hash
public class UserDto
{
    public int Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Address { get; init; }

    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }
}
