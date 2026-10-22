using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tandem.API.Extensions;
using Tandem.Core.DTOs.Banks;
using Tandem.Core.Interfaces.Services;

namespace Tandem.API.Controllers;

// The bank list, and the logged-in user's own bank account (/api/me/...: no user id in the URL,
// so nobody can ask for someone else's account)
[ApiController]
[Route("api")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class BanksController : ControllerBase
{
    private readonly IBankService _bankService;

    public BanksController(IBankService bankService)
    {
        _bankService = bankService;
    }

    [HttpGet("banks")]
    [ProducesResponseType(typeof(IReadOnlyList<BankDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BankDto>>> GetBanks(CancellationToken cancellationToken)
    {
        return Ok(await _bankService.GetBanksAsync(cancellationToken));
    }

    [HttpGet("me/bank-account")]
    [ProducesResponseType(typeof(BankAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankAccountDto>> GetMyAccount(CancellationToken cancellationToken)
    {
        var result = await _bankService.GetMyAccountAsync(User.GetUserId(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToFailureResult(result);
    }

    [HttpPut("me/bank-account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveMyAccount(
        [FromBody] SaveBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _bankService.SaveMyAccountAsync(User.GetUserId(), request, cancellationToken);

        return result.IsSuccess ? NoContent() : this.ToFailureResult(result);
    }
}
