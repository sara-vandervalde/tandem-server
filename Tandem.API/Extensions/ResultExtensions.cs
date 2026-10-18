using Microsoft.AspNetCore.Mvc;
using Tandem.Core.Common;

namespace Tandem.API.Extensions;

// The single place where a failed service Result becomes an HTTP status code.
// Controllers call it, so none of them repeats this switch
public static class ResultExtensions
{
    public static ObjectResult ToFailureResult(this ControllerBase controller, Result result)
    {
        var statusCode = result.Status switch
        {
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            _ => throw new InvalidOperationException($"Result with status {result.Status} is not a failure.")
        };

        // Same ProblemDetails JSON shape as validation errors and unhandled exceptions
        return controller.Problem(title: result.Error, statusCode: statusCode);
    }
}
