using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tandem.API.Extensions;
using Tandem.Core.DTOs.Settlements;
using Tandem.Core.Interfaces.Services;

namespace Tandem.API.Controllers;

// Closing an event's account and the payments it creates
[ApiController]
[Route("api")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlementService;

    public SettlementsController(ISettlementService settlementService)
    {
        _settlementService = settlementService;
    }

    // The live estimate: "if the account were closed now, who would pay whom"
    [HttpGet("events/{eventId:int}/settlement-preview")]
    [ProducesResponseType(typeof(SettlementPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SettlementPreviewDto>> GetPreview(
        [FromRoute] int eventId,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.GetPreviewAsync(
            eventId, User.GetUserId(), User.IsInRole("Admin"), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    // Closing the account creates the payments - a new resource, so POST and 201
    [HttpPost("events/{eventId:int}/settlements")]
    [ProducesResponseType(typeof(IReadOnlyList<SettlementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<SettlementDto>>> Close(
        [FromRoute] int eventId,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.CloseAsync(eventId, User.GetUserId(), cancellationToken);

        return result.IsSuccess
            ? Created("/api/me/settlements", result.Value)
            : this.ToFailureResult(result);
    }

    // "My payments": what I pay (with where to transfer) and what I receive
    [HttpGet("me/settlements")]
    [ProducesResponseType(typeof(IReadOnlyList<MySettlementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MySettlementDto>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _settlementService.GetMineAsync(User.GetUserId(), cancellationToken));
    }

    // "I paid" - the money itself moves outside the system
    [HttpPatch("settlements/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPaid(
        [FromRoute] int id,
        [FromBody] UpdateSettlementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _settlementService.SetPaidAsync(id, request.IsPaid!.Value, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }
}
