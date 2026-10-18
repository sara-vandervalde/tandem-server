using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tandem.API.Extensions;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Common;
using Tandem.Core.DTOs.Users;
using Tandem.Core.Interfaces.Services;

namespace Tandem.API.Controllers;

// System administration: only the Admin role. A Member gets 403, a caller without a token gets 401
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetUsersAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    // Block (IsActive = false) or release (IsActive = true) a user
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.SetActiveAsync(id, request.IsActive!.Value, User.GetUserId(), cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }
}
