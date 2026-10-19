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
