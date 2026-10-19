using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tandem.API.Extensions;
using Tandem.API.Filters;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;
using Tandem.Core.Enums;
using Tandem.Core.Interfaces.Services;

namespace Tandem.API.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    // ?groupId=&status=&search=&from=&to=&sortBy=date|title|status&sortDir=asc|desc&page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<EventListItemDto>>> GetEvents(
        [FromQuery] EventQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEventsAsync(query, User.GetUserId(), User.IsInRole("Admin"), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    // The group calendar. The leading "/" makes the route absolute, so it is nested under the group.
    // [RequireGroupMember] checks membership before the action runs
    [HttpGet("/api/groups/{groupId:int}/events")]
    [RequireGroupMember]
    [ProducesResponseType(typeof(IReadOnlyList<EventListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EventListItemDto>>> GetCalendar(
        [FromRoute] int groupId,
        [FromQuery] CalendarQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.GetCalendarAsync(groupId, query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailsDto>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetByIdAsync(id, User.GetUserId(), User.IsInRole("Admin"), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.UpdateAsync(id, request, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }

    // "I am the organizer". 200 also when it is already yours; 409 when someone else got it first
    [HttpPut("{id:int}/organizer")]
    [ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailsDto>> ClaimOrganizer(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.ClaimOrganizerAsync(id, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    // One action for /venue, /gift and /meal: the route constraint allows only these three,
    // and MVC turns the text into the ResponsibilityArea enum (case does not matter)
    [HttpPut("{id:int}/{area:regex(^(venue|gift|meal)$)}/responsible")]
    [ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailsDto>> ClaimResponsibility(
        [FromRoute] int id,
        [FromRoute] ResponsibilityArea area,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.ClaimResponsibilityAsync(id, area, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    // "I give it up": the area becomes free again
    [HttpDelete("{id:int}/{area:regex(^(venue|gift|meal)$)}/responsible")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReleaseResponsibility(
        [FromRoute] int id,
        [FromRoute] ResponsibilityArea area,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.ReleaseResponsibilityAsync(id, area, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }

    // Who shares the cost when the split method is SelectedMembers
    [HttpPut("{id:int}/participants")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetParticipants(
        [FromRoute] int id,
        [FromBody] SetParticipantsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.SetParticipantsAsync(id, request, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }

    // Cancel the event: { "status": "Cancelled" }
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateEventStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.CancelAsync(id, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _eventService.DeleteAsync(id, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailsDto>> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.CreateAsync(request, User.GetUserId(), cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToFailureResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }
}
