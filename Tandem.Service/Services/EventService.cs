using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;
using Tandem.Service.Authorization;

namespace Tandem.Service.Services;

public class EventService : IEventService
{
    private const string EventNotFoundMessage = "האירוע לא נמצא";
    private const int MaxCalendarDays = 366;

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

    public async Task<Result<IReadOnlyList<EventListItemDto>>> GetCalendarAsync(
        int groupId, CalendarQuery query, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var access = await _groupAccess.EnsureMemberOrAdminAsync(groupId, userId, isAdmin, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToFailure<IReadOnlyList<EventListItemDto>>();
        }

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

    // A date without a time zone is taken as UTC; a local date is converted
    private static DateTime ToUtc(DateTime date) => date.Kind switch
    {
        DateTimeKind.Utc => date,
        DateTimeKind.Local => date.ToUniversalTime(),
        _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
    };
}
