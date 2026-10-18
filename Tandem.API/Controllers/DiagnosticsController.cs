using Microsoft.AspNetCore.Mvc;

namespace Tandem.API.Controllers;

// Development-only tools for checking the infrastructure. In any other environment the endpoints do not exist (404)
[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public DiagnosticsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // Throws on purpose, to show ExceptionHandlingMiddleware at work:
    // a uniform JSON response with a CorrelationId, and an Error line with the same id in the log
    [HttpGet("error")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ThrowError()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        throw new InvalidOperationException("Intentional error for testing the error handling middleware.");
    }
}
