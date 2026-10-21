using Tandem.Core.Common;
using Tandem.Core.DTOs.Items;

namespace Tandem.Core.Interfaces.Services;

public interface IItemService
{
    // Group members and Admin. Private items only for the meal's responsible member
    Task<Result<IReadOnlyList<ItemDto>>> GetForEventAsync(
        int eventId, int userId, bool isAdmin, CancellationToken cancellationToken);

    // Food: the meal's responsible member, only for a Shared or SelfMade meal (SelfMade = private).
    // Equipment: the venue's responsible member, unless the caterer brings tableware
    Task<Result<ItemDto>> CreateAsync(
        int eventId, CreateItemRequest request, int userId, CancellationToken cancellationToken);

    // Whoever added the item, or whoever is responsible for its area now
    Task<Result> DeleteAsync(int itemId, int userId, CancellationToken cancellationToken);

    // "I'll bring it": any group member, in Solo too. Public items only. Two at once - one wins, the other gets 409
    Task<Result<ItemDto>> ClaimAsync(int itemId, int userId, CancellationToken cancellationToken);

    // Whoever is bringing it, or whoever added it / is responsible for its area
    Task<Result> ReleaseAsync(int itemId, int userId, CancellationToken cancellationToken);
}
