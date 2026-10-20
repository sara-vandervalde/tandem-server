using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tandem.API.Extensions;
using Tandem.Core.DTOs.Items;
using Tandem.Core.Interfaces.Services;

namespace Tandem.API.Controllers;

// The "what to bring" list. Listing and adding are nested under the event; one item is addressed by its own id
[ApiController]
[Route("api/items")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }

    // The leading "/" makes the route absolute, so it is nested under the event
    [HttpGet("/api/events/{eventId:int}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> GetForEvent(
        [FromRoute] int eventId,
        CancellationToken cancellationToken)
    {
        var result = await _itemService.GetForEventAsync(
            eventId, User.GetUserId(), User.IsInRole("Admin"), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    // 201 with the new item; the Location header points to the event's list
    [HttpPost("/api/events/{eventId:int}/items")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ItemDto>> Create(
        [FromRoute] int eventId,
        [FromBody] CreateItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _itemService.CreateAsync(eventId, request, User.GetUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToFailureResult(result);
        }

        return CreatedAtAction(nameof(GetForEvent), new { eventId }, result.Value);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _itemService.DeleteAsync(id, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }
}
