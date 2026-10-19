using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Events;

// Body of PUT /api/events/{id}/participants: the members who share the cost (replaces the whole list)
public class SetParticipantsRequest
{
    [Required(ErrorMessage = "יש לבחור לפחות משתתף/ת אחד/ת")]
    [Length(1, 100, ErrorMessage = "יש לבחור בין 1 ל-100 משתתפים")]
    public List<int> UserIds { get; init; } = [];
}
