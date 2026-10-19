using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Exceptions;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;
using Tandem.Service.Authorization;

namespace Tandem.Service.Services;

public class EventService : IEventService
{
    private const string EventNotFoundMessage = "האירוע לא נמצא";
    private const int MaxCalendarDays = 366;
    private const string OrganizerTakenMessage = "חבר/ה אחר/ת כבר לקח/ה אחריות על האירוע";
    private const string OrganizerOnlyMessage = "רק אחראי/ת האירוע יכול/ה לבצע פעולה זו";
    private const string ClosedEventMessage = "אי אפשר לשנות אחריות באירוע שנסגר או בוטל";
    private const string AreaChangedMessage = "התחום השתנה בינתיים. רעננו את הדף ונסו שוב";

    private readonly IEventRepository _eventRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly GroupAccessService _groupAccess;
    private readonly IMapper _mapper;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository eventRepository,
        IGroupRepository groupRepository,
        GroupAccessService groupAccess,
        IMapper mapper,
        ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _groupRepository = groupRepository;
        _groupAccess = groupAccess;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<EventDetailsDto>> CreateAsync(
        CreateEventRequest request, int userId, CancellationToken cancellationToken)
    {
        var groupId = request.GroupId!.Value;
        var access = await _groupAccess.EnsureMemberAsync(groupId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToFailure<EventDetailsDto>();
        }

        // PostgreSQL stores timestamptz and rejects dates that are not UTC
        var eventDate = ToUtc(request.EventDate!.Value);
        if (eventDate.Date < DateTime.UtcNow.Date)
        {
            return Result<EventDetailsDto>.ValidationError("אי אפשר ליצור אירוע בתאריך שעבר");
        }

        // "Claiming is always an update": venue and meal exist from the start with no responsible member,
        // so later claims update an existing row that the concurrency token protects
        var evt = new Event
        {
            GroupId = groupId,
            CreatedById = userId,
            Title = request.Title.Trim(),
            EventDate = eventDate,
            OrganizerId = request.IsOrganizer ? userId : null,
            Status = request.IsOrganizer ? EventStatus.Organizing : EventStatus.Draft,
            Venue = new Venue(),
            Meal = new Meal()
        };

        _eventRepository.Add(evt);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Event {EventId} created in group {GroupId} by user {UserId} ({Status})",
            evt.Id, groupId, userId, evt.Status);

        var created = await _eventRepository.GetDetailsAsync(evt.Id, cancellationToken);
        return Result<EventDetailsDto>.Success(_mapper.Map<EventDetailsDto>(created!));
    }

    public async Task<Result<PagedResult<EventListItemDto>>> GetEventsAsync(
        EventQuery query, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (query.GroupId is int groupId)
        {
            var access = await _groupAccess.EnsureMemberOrAdminAsync(groupId, userId, isAdmin, cancellationToken);
            if (!access.IsSuccess)
            {
                return access.ToFailure<PagedResult<EventListItemDto>>();
            }
        }

        var from = query.From is DateTime f ? ToUtc(f) : (DateTime?)null;
        var to = query.To is DateTime t ? ToUtc(t) : (DateTime?)null;
        if (from > to)
        {
            return Result<PagedResult<EventListItemDto>>.ValidationError("תאריך ההתחלה מאוחר מתאריך הסיום");
        }

        var normalized = new EventQuery
        {
            GroupId = query.GroupId,
            Status = query.Status,
            Search = query.Search,
            From = from,
            To = to,
            SortBy = query.SortBy,
            SortDir = query.SortDir,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var page = await _eventRepository.GetPagedAsync(normalized, isAdmin ? null : userId, cancellationToken);

        return Result<PagedResult<EventListItemDto>>.Success(new PagedResult<EventListItemDto>
        {
            Items = _mapper.Map<List<EventListItemDto>>(page.Items),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        });
    }

    // Access (member or Admin) is checked before this runs, by the [RequireGroupMember] filter on the endpoint
    public async Task<Result<IReadOnlyList<EventListItemDto>>> GetCalendarAsync(
        int groupId, CalendarQuery query, CancellationToken cancellationToken)
    {
        var from = ToUtc(query.From!.Value);
        var to = ToUtc(query.To!.Value);
        if (from > to || (to - from).TotalDays > MaxCalendarDays)
        {
            return Result<IReadOnlyList<EventListItemDto>>.ValidationError(
                "טווח התאריכים לא תקין (עד שנה אחת, וההתחלה לפני הסיום)");
        }

        var events = await _eventRepository.GetForGroupInRangeAsync(groupId, from, to, cancellationToken);
        return Result<IReadOnlyList<EventListItemDto>>.Success(_mapper.Map<List<EventListItemDto>>(events));
    }

    public async Task<Result<EventDetailsDto>> GetByIdAsync(
        int eventId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var evt = await _eventRepository.GetDetailsAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result<EventDetailsDto>.NotFound(EventNotFoundMessage);
        }

        if (!isAdmin && !await _groupRepository.IsMemberAsync(evt.GroupId, userId, cancellationToken))
        {
            return Result<EventDetailsDto>.Forbidden("אין לך גישה לאירוע זה");
        }

        return Result<EventDetailsDto>.Success(_mapper.Map<EventDetailsDto>(evt));
    }

    public async Task<Result<EventDetailsDto>> ClaimOrganizerAsync(
        int eventId, int userId, CancellationToken cancellationToken)
    {
        // 1. Load WITH tracking: EF remembers the row's version (xmin) as it was at this moment
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result<EventDetailsDto>.NotFound(EventNotFoundMessage);
        }

        // 2. Permission: members only (the Admin views but does not take part)
        if (!await _groupRepository.IsMemberAsync(evt.GroupId, userId, cancellationToken))
        {
            return Result<EventDetailsDto>.Forbidden("רק חברי הקבוצה יכולים לקחת אחריות על האירוע");
        }

        if (evt.Status is EventStatus.Settled or EventStatus.Cancelled)
        {
            return Result<EventDetailsDto>.Conflict("אי אפשר לקחת אחריות על אירוע שנסגר או בוטל");
        }

        // 3. Business check: already mine = nothing to do (a repeated PUT gives the same result)
        if (evt.OrganizerId != userId)
        {
            if (evt.OrganizerId is not null)
            {
                return Result<EventDetailsDto>.Conflict(OrganizerTakenMessage);
            }

            // 4. Claim and save. Two members who passed step 3 at the same moment both reach here:
            //    the first save wins, the second one's UPDATE finds a different version and is rejected
            evt.OrganizerId = userId;
            evt.Status = EventStatus.Organizing;

            try
            {
                await _eventRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                // 5. Lost the race: an expected situation, handled - so Warning, not Error
                _logger.LogWarning("Concurrency conflict: user {UserId} lost the race to organize event {EventId}",
                    userId, eventId);
                return Result<EventDetailsDto>.Conflict(OrganizerTakenMessage);
            }

            _logger.LogInformation("User {UserId} is now the organizer of event {EventId}", userId, eventId);
        }

        var updated = await _eventRepository.GetDetailsAsync(eventId, cancellationToken);
        return Result<EventDetailsDto>.Success(_mapper.Map<EventDetailsDto>(updated!));
    }

    public async Task<Result<EventDetailsDto>> ClaimResponsibilityAsync(
        int eventId, ResponsibilityArea area, int userId, CancellationToken cancellationToken)
    {
        // 1. Load WITH tracking: EF remembers the version (xmin) of the venue / gift / meal row
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result<EventDetailsDto>.NotFound(EventNotFoundMessage);
        }

        // 2. Permission: members only; in Solo the organizer takes every area alone
        if (!await _groupRepository.IsMemberAsync(evt.GroupId, userId, cancellationToken))
        {
            return Result<EventDetailsDto>.Forbidden("רק חברי הקבוצה יכולים לקחת אחריות");
        }

        if (evt.OrganizationMode == OrganizationMode.Solo && evt.OrganizerId != userId)
        {
            return Result<EventDetailsDto>.Forbidden("באירוע במצב Solo רק אחראי/ת האירוע לוקח/ת את התחומים");
        }

        // 3. Business checks
        if (evt.OrganizerId is null)
        {
            return Result<EventDetailsDto>.Conflict("קודם מישהו/י צריך/ה לקחת אחריות על האירוע");
        }

        if (evt.Status is EventStatus.Settled or EventStatus.Cancelled)
        {
            return Result<EventDetailsDto>.Conflict(ClosedEventMessage);
        }

        var responsibility = GetResponsibility(evt, area);
        if (responsibility is null)
        {
            return Result<EventDetailsDto>.NotFound("באירוע הזה אין מתנה");
        }

        // Already mine = nothing to do (a repeated PUT gives the same result)
        if (responsibility.ResponsibleId != userId)
        {
            if (responsibility.ResponsibleId is not null)
            {
                return Result<EventDetailsDto>.Conflict(AreaTakenMessage(area));
            }

            // 4. Claim and save. Two members who passed step 3 at the same moment both reach here:
            //    the first save wins, the second one's UPDATE finds a different version and is rejected
            responsibility.ResponsibleId = userId;

            try
            {
                await _eventRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                // 5. Lost the race: an expected situation, handled - so Warning, not Error
                _logger.LogWarning("Concurrency conflict: user {UserId} lost the race for the {Area} of event {EventId}",
                    userId, area, eventId);
                return Result<EventDetailsDto>.Conflict(AreaTakenMessage(area));
            }

            _logger.LogInformation("User {UserId} is now responsible for the {Area} of event {EventId}",
                userId, area, eventId);
        }

        var updated = await _eventRepository.GetDetailsAsync(eventId, cancellationToken);
        return Result<EventDetailsDto>.Success(_mapper.Map<EventDetailsDto>(updated!));
    }

    public async Task<Result> ReleaseResponsibilityAsync(
        int eventId, ResponsibilityArea area, int userId, CancellationToken cancellationToken)
    {
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result.NotFound(EventNotFoundMessage);
        }

        if (evt.Status is EventStatus.Settled or EventStatus.Cancelled)
        {
            return Result.Conflict(ClosedEventMessage);
        }

        var responsibility = GetResponsibility(evt, area);
        if (responsibility is null)
        {
            return Result.NotFound("באירוע הזה אין מתנה");
        }

        if (responsibility.ResponsibleId is null)
        {
            return Result.Conflict("אין אחראי/ת על התחום הזה");
        }

        if (responsibility.ResponsibleId != userId)
        {
            return Result.Forbidden("רק מי שלקח/ה את האחריות יכול/ה לשחרר אותה");
        }

        // The price is what the responsible member paid, so it leaves with them
        responsibility.ResponsibleId = null;
        responsibility.Price = null;

        try
        {
            await _eventRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict: user {UserId} could not release the {Area} of event {EventId}",
                userId, area, eventId);
            return Result.Conflict(AreaChangedMessage);
        }

        _logger.LogInformation("User {UserId} released the {Area} of event {EventId}", userId, area, eventId);
        return Result.Success();
    }

    public async Task<Result> UpdateVenueAsync(
        int eventId, UpdateVenueRequest request, int userId, CancellationToken cancellationToken)
    {
        var access = await GetAreaForEditAsync(eventId, ResponsibilityArea.Venue, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        var venue = (Venue)access.Value!;
        venue.Address = request.Address?.Trim();
        venue.Price = request.Price;

        return await SaveAreaAsync(eventId, ResponsibilityArea.Venue, userId, cancellationToken);
    }

    public async Task<Result> UpdateGiftAsync(
        int eventId, UpdateGiftRequest request, int userId, CancellationToken cancellationToken)
    {
        var access = await GetAreaForEditAsync(eventId, ResponsibilityArea.Gift, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        var gift = (Gift)access.Value!;
        gift.Description = request.Description?.Trim();
        gift.Price = request.Price;

        return await SaveAreaAsync(eventId, ResponsibilityArea.Gift, userId, cancellationToken);
    }

    public async Task<Result> UpdateMealAsync(
        int eventId, UpdateMealRequest request, int userId, CancellationToken cancellationToken)
    {
        var access = await GetAreaForEditAsync(eventId, ResponsibilityArea.Meal, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access;
        }

        var meal = (Meal)access.Value!;
        meal.MealType = request.MealType;
        // A caterer's name means nothing for a shared or self-made meal
        meal.CateringName = request.MealType == MealType.Catering ? request.CateringName?.Trim() : null;
        meal.Price = request.Price;

        return await SaveAreaAsync(eventId, ResponsibilityArea.Meal, userId, cancellationToken);
    }

    public async Task<Result> UpdateAsync(
        int eventId, UpdateEventRequest request, int userId, CancellationToken cancellationToken)
    {
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        var check = CheckOrganizerCanChange(evt, userId);
        if (check is not null)
        {
            return check;
        }

        var eventDate = ToUtc(request.EventDate!.Value);
        if (eventDate.Date < DateTime.UtcNow.Date && eventDate != evt!.EventDate)
        {
            return Result.ValidationError("אי אפשר להעביר אירוע לתאריך שעבר");
        }

        evt!.Title = request.Title.Trim();
        evt.EventDate = eventDate;
        evt.OrganizationMode = request.OrganizationMode!.Value;
        evt.SplitMethod = request.SplitMethod!.Value;

        // The gift row exists only while HasGift is on. Turning it on/off is the organizer's own action - no race
        if (request.HasGift && evt.Gift is null)
        {
            evt.Gift = new Gift();
        }
        else if (!request.HasGift && evt.Gift is not null)
        {
            evt.Gift = null;
        }
        evt.HasGift = request.HasGift;

        // The chosen participants matter only for SelectedMembers
        if (evt.SplitMethod != SplitMethod.SelectedMembers)
        {
            evt.Participants.Clear();
        }

        await _eventRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Event {EventId} updated by user {UserId}", eventId, userId);

        return Result.Success();
    }

    public async Task<Result> SetParticipantsAsync(
        int eventId, SetParticipantsRequest request, int userId, CancellationToken cancellationToken)
    {
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        var check = CheckOrganizerCanChange(evt, userId);
        if (check is not null)
        {
            return check;
        }

        if (evt!.SplitMethod != SplitMethod.SelectedMembers)
        {
            return Result.Conflict("בחירת משתתפים אפשרית רק בשיטת החלוקה \"משתתפים נבחרים\"");
        }

        var chosen = request.UserIds.Distinct().ToList();
        var memberIds = await _groupRepository.GetMemberIdsAsync(evt.GroupId, cancellationToken);
        if (chosen.Except(memberIds).Any())
        {
            return Result.ValidationError("אפשר לבחור רק חברים בקבוצה");
        }

        // Replace the list by its difference: rows no longer chosen are deleted, new ones are inserted,
        // and members who stay are left untouched (re-adding the same key would clash with the tracked row)
        foreach (var removed in evt.Participants.Where(p => !chosen.Contains(p.UserId)).ToList())
        {
            evt.Participants.Remove(removed);
        }

        var existingIds = evt.Participants.Select(p => p.UserId).ToList();
        foreach (var participantId in chosen.Except(existingIds))
        {
            evt.Participants.Add(new EventParticipant { EventId = eventId, UserId = participantId });
        }

        await _eventRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Event {EventId}: {Count} participants set by user {UserId}",
            eventId, chosen.Count, userId);

        return Result.Success();
    }

    public async Task<Result> CancelAsync(int eventId, int userId, CancellationToken cancellationToken)
    {
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result.NotFound(EventNotFoundMessage);
        }

        if (evt.OrganizerId != userId)
        {
            return Result.Forbidden(OrganizerOnlyMessage);
        }

        if (evt.Status == EventStatus.Settled)
        {
            return Result.Conflict("אי אפשר לבטל אירוע שהחשבון שלו כבר נסגר");
        }

        // Cancelling twice is not an error
        if (evt.Status != EventStatus.Cancelled)
        {
            evt.Status = EventStatus.Cancelled;
            await _eventRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Event {EventId} cancelled by user {UserId}", eventId, userId);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int eventId, int userId, CancellationToken cancellationToken)
    {
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result.NotFound(EventNotFoundMessage);
        }

        var allowed = evt.OrganizerId is null
            ? evt.CreatedById == userId || evt.Group.ManagerId == userId
            : evt.OrganizerId == userId;

        if (!allowed)
        {
            return Result.Forbidden(evt.OrganizerId is null
                ? "טיוטה יכולים למחוק רק מי שיצר/ה אותה או מנהל הקבוצה"
                : OrganizerOnlyMessage);
        }

        _eventRepository.Remove(evt);
        await _eventRepository.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Event {EventId} deleted by user {UserId}", eventId, userId);

        return Result.Success();
    }

    // Shared by the organizer's edit actions: exists, is the organizer, still open
    private static Result? CheckOrganizerCanChange(Event? evt, int userId)
    {
        if (evt is null)
        {
            return Result.NotFound(EventNotFoundMessage);
        }

        if (evt.OrganizerId != userId)
        {
            return Result.Forbidden(OrganizerOnlyMessage);
        }

        return evt.Status is EventStatus.Settled or EventStatus.Cancelled
            ? Result.Conflict("אי אפשר לשנות אירוע שנסגר או בוטל")
            : null;
    }

    // A date without a time zone is taken as UTC; a local date is converted
    // Shared by the three updates. Loads WITH tracking, so the save is protected by the area's version
    private async Task<Result<IResponsibility>> GetAreaForEditAsync(
        int eventId, ResponsibilityArea area, int userId, CancellationToken cancellationToken)
    {
        var evt = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result<IResponsibility>.NotFound(EventNotFoundMessage);
        }

        // After settling, the prices are locked: the payments were calculated from them
        if (evt.Status is EventStatus.Settled or EventStatus.Cancelled)
        {
            return Result<IResponsibility>.Conflict("אי אפשר לעדכן פרטים ומחיר באירוע שנסגר או בוטל");
        }

        var responsibility = GetResponsibility(evt, area);
        if (responsibility is null)
        {
            return Result<IResponsibility>.NotFound("באירוע הזה אין מתנה");
        }

        return responsibility.ResponsibleId == userId
            ? Result<IResponsibility>.Success(responsibility)
            : Result<IResponsibility>.Forbidden("רק האחראי/ת על התחום יכול/ה לעדכן את הפרטים והמחיר");
    }

    // If the area changed since it was loaded (e.g. released at the same moment), the save is rejected: 409
    private async Task<Result> SaveAreaAsync(
        int eventId, ResponsibilityArea area, int userId, CancellationToken cancellationToken)
    {
        try
        {
            await _eventRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict: user {UserId} could not update the {Area} of event {EventId}",
                userId, area, eventId);
            return Result.Conflict(AreaChangedMessage);
        }

        _logger.LogInformation("User {UserId} updated the {Area} of event {EventId}", userId, area, eventId);
        return Result.Success();
    }

    // The interface lets one piece of code claim or release any of the three areas.
    // Null only for the gift of an event without a gift
    private static IResponsibility? GetResponsibility(Event evt, ResponsibilityArea area) => area switch
    {
        ResponsibilityArea.Venue => evt.Venue,
        ResponsibilityArea.Gift => evt.Gift,
        ResponsibilityArea.Meal => evt.Meal,
        _ => throw new ArgumentOutOfRangeException(nameof(area))
    };

    private static string AreaTakenMessage(ResponsibilityArea area)
    {
        var name = area switch
        {
            ResponsibilityArea.Venue => "המקום",
            ResponsibilityArea.Gift => "המתנה",
            _ => "הארוחה"
        };
        return $"חבר/ה אחר/ת כבר לקח/ה אחריות על {name}";
    }

    private static DateTime ToUtc(DateTime date) => date.Kind switch
    {
        DateTimeKind.Utc => date,
        DateTimeKind.Local => date.ToUniversalTime(),
        _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
    };
}
