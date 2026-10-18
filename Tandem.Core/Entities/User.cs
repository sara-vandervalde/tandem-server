using Tandem.Core.Enums;

namespace Tandem.Core.Entities;

public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    // Always stored in lowercase, so invitations match regardless of letter case
    public string Email { get; set; } = string.Empty;

    // Never the plain password - only the hash
    public string PasswordHash { get; set; } = string.Empty;

    public string? Address { get; set; }

    public UserRole Role { get; set; } = UserRole.Member;

    // Users are never deleted, only blocked
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
