using Tandem.Core.Common;
using Tandem.Core.DTOs.Common;
using Tandem.Core.DTOs.Messages;

namespace Tandem.Core.Interfaces.Services;

public interface IMessageService
{
    // Group members and the Admin read; 404 if the event does not exist
    Task<Result<PagedResult<MessageDto>>> GetForEventAsync(
        int eventId, PaginationQuery query, int userId, bool isAdmin, CancellationToken cancellationToken);

    // Only group members write (the Admin watches but does not take part)
    Task<Result<MessageDto>> CreateAsync(
        int eventId, CreateMessageRequest request, int userId, CancellationToken cancellationToken);
}
