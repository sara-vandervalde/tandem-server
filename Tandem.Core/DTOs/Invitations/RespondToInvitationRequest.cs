using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Invitations;

// Body of PATCH /api/invitations/{id}: true accepts (joins the group), false declines
public class RespondToInvitationRequest
{
    // Nullable so a missing value is rejected instead of silently becoming false (= decline)
    [Required(ErrorMessage = "יש לציין אישור או דחייה")]
    public bool? Accept { get; init; }
}
