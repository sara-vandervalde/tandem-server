using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tandem.API.Extensions;
using Tandem.Core.DTOs.Groups;
using Tandem.Core.Interfaces.Services;

namespace Tandem.API.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    // Admin: all groups. Member: the groups they belong to
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> GetGroups(CancellationToken cancellationToken)
    {
        var groups = await _groupService.GetGroupsAsync(User.GetUserId(), User.IsInRole("Admin"), cancellationToken);
        return Ok(groups);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GroupDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDetailsDto>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _groupService.GetByIdAsync(id, User.GetUserId(), User.IsInRole("Admin"), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GroupDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupDetailsDto>> Create(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _groupService.CreateAsync(request, User.GetUserId(), cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToFailureResult(result);
        }

        // 201 with a Location header pointing to GET /api/groups/{id}
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    // The manager hands management to another member
    [HttpPut("{id:int}/manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferManager(
        [FromRoute] int id,
        [FromBody] TransferManagerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _groupService.TransferManagerAsync(
            id, request.UserId!.Value, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }

    // Leave the group (userId = yourself) or, as the manager, remove a member
    [HttpDelete("{id:int}/members/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] int id,
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _groupService.RemoveMemberAsync(id, userId, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }
}
