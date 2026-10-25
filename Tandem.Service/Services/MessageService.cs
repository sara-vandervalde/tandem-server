using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Common;
using Tandem.Core.DTOs.Messages;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;
using Tandem.Service.Authorization;

namespace Tandem.Service.Services;

public class MessageService : IMessageService
{
    private const string EventNotFoundMessage = "האירוע לא נמצא";

    private readonly IMessageRepository _messageRepository;
    private readonly IEventRepository _eventRepository;
    private readonly GroupAccessService _groupAccess;
    private readonly IMapper _mapper;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        IMessageRepository messageRepository,
        IEventRepository eventRepository,
        GroupAccessService groupAccess,
        IMapper mapper,
        ILogger<MessageService> logger)
    {
        _messageRepository = messageRepository;
        _eventRepository = eventRepository;
        _groupAccess = groupAccess;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PagedResult<MessageDto>>> GetForEventAsync(
        int eventId, PaginationQuery query, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var groupId = await _eventRepository.GetGroupIdAsync(eventId, cancellationToken);
        if (groupId is null)
        {
            return Result<PagedResult<MessageDto>>.NotFound(EventNotFoundMessage);
        }

        var access = await _groupAccess.EnsureMemberOrAdminAsync(groupId.Value, userId, isAdmin, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToFailure<PagedResult<MessageDto>>();
        }

        var page = await _messageRepository.GetPagedAsync(eventId, query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<MessageDto>>.Success(new PagedResult<MessageDto>
        {
            Items = _mapper.Map<List<MessageDto>>(page.Items),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        });
    }

    public async Task<Result<MessageDto>> CreateAsync(
        int eventId, CreateMessageRequest request, int userId, CancellationToken cancellationToken)
    {
        var groupId = await _eventRepository.GetGroupIdAsync(eventId, cancellationToken);
        if (groupId is null)
        {
            return Result<MessageDto>.NotFound(EventNotFoundMessage);
        }

        var access = await _groupAccess.EnsureMemberAsync(groupId.Value, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToFailure<MessageDto>();
        }

        var text = request.Text.Trim();
        if (text.Length == 0)
        {
            return Result<MessageDto>.ValidationError("יש לכתוב הודעה");
        }

        var message = new EventMessage
        {
            EventId = eventId,
            UserId = userId,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };
        _messageRepository.Add(message);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        // The text itself is not logged - only who wrote where
        _logger.LogInformation("User {UserId} wrote message {MessageId} on event {EventId}", userId, message.Id, eventId);

        // Read back with the writer's name for the response
        var saved = await _messageRepository.GetByIdAsync(message.Id, cancellationToken);
        return Result<MessageDto>.Success(_mapper.Map<MessageDto>(saved));
    }
}
