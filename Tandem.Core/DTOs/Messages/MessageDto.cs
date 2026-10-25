namespace Tandem.Core.DTOs.Messages;

// One message on the event's board
public class MessageDto
{
    public int Id { get; init; }

    public int EventId { get; init; }

    public int UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
