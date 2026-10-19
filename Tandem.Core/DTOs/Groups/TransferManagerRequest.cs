using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Groups;

// Body of PUT /api/groups/{id}/manager: the member who becomes the new manager
public class TransferManagerRequest
{
    [Required(ErrorMessage = "יש לבחור את המנהל החדש")]
    [Range(1, int.MaxValue, ErrorMessage = "מזהה משתמש לא תקין")]
    public int? UserId { get; init; }
}
