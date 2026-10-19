using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tandem.API.Extensions;
using Tandem.Core.DTOs.Invitations;
using Tandem.Core.Interfaces.Services;

namespace Tandem.API.Controllers;

// Invitations live under a group when they are created and listed (/api/groups/{groupId}/invitations)
[ApiController]
[Route("api")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationService;

    public InvitationsController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    // 201 when at least one invitation was sent; 200 when every email was skipped
    [HttpPost("groups/{groupId:int}/invitations")]
    [ProducesResponseType(typeof(CreateInvitationsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreateInvitationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateInvitationsResponse>> Create(
        [FromRoute] int groupId,
        [FromBody] CreateInvitationsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _invitationService.CreateAsync(groupId, request, User.GetUserId(), cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToFailureResult(result);
        }

        return result.Value!.Invited.Count > 0
            ? CreatedAtAction(nameof(GetForGroup), new { groupId }, result.Value)
            : Ok(result.Value);
    }

    [HttpGet("groups/{groupId:int}/invitations")]
    [ProducesResponseType(typeof(IReadOnlyList<InvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<InvitationDto>>> GetForGroup(
        [FromRoute] int groupId,
        CancellationToken cancellationToken)
    {
        var result = await _invitationService.GetForGroupAsync(groupId, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    // The logged-in user's pending invitations (shown at the top of the home page)
    [HttpGet("me/invitations")]
    [ProducesResponseType(typeof(IReadOnlyList<InvitationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvitationDto>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _invitationService.GetMyInvitationsAsync(User.GetEmail(), cancellationToken));
    }

    // Accept (joins the group) or decline. Only the invited user
    [HttpPatch("invitations/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Respond(
        [FromRoute] int id,
        [FromBody] RespondToInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _invitationService.RespondAsync(
            id, request.Accept!.Value, User.GetUserId(), User.GetEmail(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }

    // The manager cancels a pending invitation
    [HttpDelete("invitations/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _invitationService.CancelAsync(id, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }
}
