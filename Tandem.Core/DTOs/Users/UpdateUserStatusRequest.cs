using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Users;

// Body of PATCH /api/users/{id}: false blocks the user, true releases the block
public class UpdateUserStatusRequest
{
    // Nullable so that a missing value is rejected instead of silently becoming false (= blocked)
    [Required(ErrorMessage = "יש לציין האם המשתמש פעיל")]
    public bool? IsActive { get; init; }
}
