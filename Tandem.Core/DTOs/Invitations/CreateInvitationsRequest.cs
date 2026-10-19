using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Invitations;

// The manager invites one or more people by email in a single request
public class CreateInvitationsRequest
{
    [Required(ErrorMessage = "יש להזין לפחות כתובת אימייל אחת")]
    [Length(1, 20, ErrorMessage = "אפשר להזמין בין 1 ל-20 כתובות בכל פעם")]
    public List<string> Emails { get; init; } = [];
}
