using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tandem.API.Extensions;
using Tandem.Core.DTOs.Events;
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

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailsDto>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetByIdAsync(id, User.GetUserId(), User.IsInRole("Admin"), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
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
