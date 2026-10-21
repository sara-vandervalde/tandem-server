using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Items;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Exceptions;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;
using Tandem.Service.Authorization;
using Tandem.Service.Rules;

namespace Tandem.Service.Services;

// The "what to bring" list: food (the meal's responsible member) and equipment (the venue's)
public class ItemService : IItemService
{
    private const string EventNotFoundMessage = "האירוע לא נמצא";
    private const string ItemNotFoundMessage = "הפריט לא נמצא";
    private const string ClosedEventMessage = "אי אפשר לשנות את הרשימה באירוע שנסגר או בוטל";
    private const string ItemTakenMessage = "מישהו/י אחר/ת כבר מביא/ה את הפריט הזה";
    private const string ItemChangedMessage = "הפריט השתנה בינתיים. רעננו את הדף ונסו שוב";

    private readonly IItemRepository _itemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly GroupAccessService _groupAccess;
    private readonly IMapper _mapper;
    private readonly ILogger<ItemService> _logger;

    public ItemService(
        IItemRepository itemRepository,
        IEventRepository eventRepository,
        GroupAccessService groupAccess,
        IMapper mapper,
        ILogger<ItemService> logger)
    {
        _itemRepository = itemRepository;
        _eventRepository = eventRepository;
        _groupAccess = groupAccess;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ItemDto>>> GetForEventAsync(
        int eventId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var groupId = await _eventRepository.GetGroupIdAsync(eventId, cancellationToken);
        if (groupId is null)
        {
            return Result<IReadOnlyList<ItemDto>>.NotFound(EventNotFoundMessage);
        }

        var access = await _groupAccess.EnsureMemberOrAdminAsync(groupId.Value, userId, isAdmin, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToFailure<IReadOnlyList<ItemDto>>();
        }

        var items = await _itemRepository.GetForEventAsync(eventId, userId, cancellationToken);
        return Result<IReadOnlyList<ItemDto>>.Success(_mapper.Map<List<ItemDto>>(items));
    }

    public async Task<Result<ItemDto>> CreateAsync(
        int eventId, CreateItemRequest request, int userId, CancellationToken cancellationToken)
    {
        // Tracked, with venue and meal: who is responsible decides who may add
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result<ItemDto>.NotFound(EventNotFoundMessage);
        }

        if (evt.Status is EventStatus.Settled or EventStatus.Cancelled)
        {
            return Result<ItemDto>.Conflict(ClosedEventMessage);
        }

        var type = request.Type!.Value;
        var isPrivate = false;

        if (type == ItemType.Food)
        {
            if (evt.Meal.ResponsibleId != userId)
            {
                return Result<ItemDto>.Forbidden("רק האחראי/ת על הארוחה יכול/ה להוסיף פריטי אוכל");
            }

            switch (evt.Meal.MealType)
            {
                case null:
                    return Result<ItemDto>.Conflict("קודם צריך לבחור את סוג הארוחה");
                case MealType.Catering:
                    return Result<ItemDto>.Conflict("בארוחת קייטרינג אין פריטי אוכל ברשימה");
                case MealType.SelfMade:
                    // The cook's private menu: nobody else sees it or claims from it
                    isPrivate = true;
                    break;
            }
        }
        else
        {
            if (evt.Venue.ResponsibleId != userId)
            {
                return Result<ItemDto>.Forbidden("רק האחראי/ת על המקום יכול/ה להוסיף כלים ועריכה");
            }

            if (evt.Meal.MealType == MealType.Catering && evt.Meal.CateringIncludesTableware)
            {
                return Result<ItemDto>.Conflict("הקייטרינג מביא כלים ועריכה, לכן אין צורך להוסיף אותם לרשימה");
            }
        }

        var item = new EventItem
        {
            EventId = eventId,
            Type = type,
            Name = request.Name.Trim(),
            IsPrivate = isPrivate,
            AddedById = userId
        };
        // Added through the event's list, so the status sees it: a new public item is not claimed yet
        evt.Items.Add(item);
        EventStatusRules.Refresh(evt, _logger);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} added {Type} item {ItemId} to event {EventId}",
            userId, type, item.Id, eventId);

        var created = await _itemRepository.GetDetailsAsync(item.Id, cancellationToken);
        return Result<ItemDto>.Success(_mapper.Map<ItemDto>(created!));
    }

    public async Task<Result> DeleteAsync(int itemId, int userId, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetForUpdateAsync(itemId, cancellationToken);

        // A private item answers 404 to anyone but the meal's responsible member: for them it does not exist
        if (item is null || (item.IsPrivate && item.Event.Meal.ResponsibleId != userId))
        {
            return Result.NotFound(ItemNotFoundMessage);
        }

        if (item.Event.Status is EventStatus.Settled or EventStatus.Cancelled)
        {
            return Result.Conflict(ClosedEventMessage);
        }

        // Whoever added it, or whoever is responsible for its area now (the previous one may have released)
        var areaResponsibleId = item.Type == ItemType.Food
            ? item.Event.Meal.ResponsibleId
            : item.Event.Venue.ResponsibleId;
        if (item.AddedById != userId && areaResponsibleId != userId)
        {
            return Result.Forbidden("רק מי שהוסיף/ה את הפריט, או האחראי/ת על התחום, יכול/ה למחוק אותו");
        }

        // If someone claimed or changed the item since it was loaded, the DELETE matches no row: 409
        // Out of the event's list too, so the status no longer waits for it
        item.Event.Items.Remove(item);
        _itemRepository.Remove(item);
        EventStatusRules.Refresh(item.Event, _logger);
        try
        {
            await _itemRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict: user {UserId} could not delete item {ItemId}", userId, itemId);
            return Result.Conflict(ItemChangedMessage);
        }

        _logger.LogInformation("User {UserId} deleted item {ItemId}", userId, itemId);
        return Result.Success();
    }

    public async Task<Result<ItemDto>> ClaimAsync(int itemId, int userId, CancellationToken cancellationToken)
    {
        // 1. Load WITH tracking: EF remembers the item's version (xmin)
        var item = await _itemRepository.GetForUpdateAsync(itemId, cancellationToken);
        if (item is null || (item.IsPrivate && item.Event.Meal.ResponsibleId != userId))
        {
            return Result<ItemDto>.NotFound(ItemNotFoundMessage);
        }

        // 2. Permission: any member, also in Solo - bringing a dish is not running the event
        var access = await _groupAccess.EnsureMemberAsync(item.Event.GroupId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToFailure<ItemDto>();
        }

        // 3. Business checks
        if (item.Event.Status is EventStatus.Settled or EventStatus.Cancelled)
        {
            return Result<ItemDto>.Conflict(ClosedEventMessage);
        }

        if (item.IsPrivate)
        {
            return Result<ItemDto>.Conflict("זה פריט בתפריט הפרטי שלך – אין צורך לתפוס אותו");
        }

        // Already mine = nothing to do (a repeated PUT gives the same result)
        if (item.AssigneeId != userId)
        {
            if (item.AssigneeId is not null)
            {
                return Result<ItemDto>.Conflict(ItemTakenMessage);
            }

            // 4. Save. If someone else claimed it after we loaded it, the UPDATE matches no row: 409
            item.AssigneeId = userId;
            EventStatusRules.Refresh(item.Event, _logger);
            try
            {
                await _itemRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning("Concurrency conflict: user {UserId} could not claim item {ItemId}", userId, itemId);
                return Result<ItemDto>.Conflict(ItemTakenMessage);
            }

            _logger.LogInformation("User {UserId} claimed item {ItemId}", userId, itemId);
        }

        var claimed = await _itemRepository.GetDetailsAsync(itemId, cancellationToken);
        return Result<ItemDto>.Success(_mapper.Map<ItemDto>(claimed!));
    }

    public async Task<Result> ReleaseAsync(int itemId, int userId, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetForUpdateAsync(itemId, cancellationToken);
        if (item is null || (item.IsPrivate && item.Event.Meal.ResponsibleId != userId))
        {
            return Result.NotFound(ItemNotFoundMessage);
        }

        if (item.Event.Status is EventStatus.Settled or EventStatus.Cancelled)
        {
            return Result.Conflict(ClosedEventMessage);
        }

        if (item.AssigneeId is null)
        {
            return Result.Conflict("אף אחד/ת לא תפס/ה את הפריט הזה");
        }

        // The one bringing it, or the one in charge of the list (e.g. a member who stopped answering)
        var areaResponsibleId = item.Type == ItemType.Food
            ? item.Event.Meal.ResponsibleId
            : item.Event.Venue.ResponsibleId;
        if (item.AssigneeId != userId && item.AddedById != userId && areaResponsibleId != userId)
        {
            return Result.Forbidden("רק מי שתפס/ה את הפריט, או האחראי/ת על התחום, יכול/ה לשחרר אותו");
        }

        item.AssigneeId = null;
        EventStatusRules.Refresh(item.Event, _logger);
        try
        {
            await _itemRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict: user {UserId} could not release item {ItemId}", userId, itemId);
            return Result.Conflict(ItemChangedMessage);
        }

        _logger.LogInformation("User {UserId} released item {ItemId}", userId, itemId);
        return Result.Success();
    }
}
