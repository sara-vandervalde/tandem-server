using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Messages;

// Body of POST /api/events/{id}/messages
public class CreateMessageRequest
{
    [Required(ErrorMessage = "יש לכתוב הודעה")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "ההודעה יכולה להכיל עד 500 תווים")]
    public string Text { get; init; } = string.Empty;
}
