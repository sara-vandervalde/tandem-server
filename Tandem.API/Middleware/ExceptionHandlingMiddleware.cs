using Microsoft.AspNetCore.Mvc;

namespace Tandem.API.Middleware;

// First in the pipeline: catches any exception that was not handled further down,
// logs it at Error level and returns one uniform JSON response (ProblemDetails) instead of a crash page.
// Expected failures (not found, conflict...) never reach here - services return them as a Result
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client closed the connection - not a server error, and there is no one to answer
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            // The CorrelationId logging scope has already closed here, so the id is read from HttpContext
            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] as string ?? context.TraceIdentifier;

            using (_logger.BeginScope(new Dictionary<string, object> { [CorrelationIdMiddleware.ItemKey] = correlationId }))
            {
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "אירעה שגיאה בלתי צפויה. נסו שוב מאוחר יותר.",
                Instance = context.Request.Path
            };
            // Lets the user report the id, so the exact log lines can be found
            problem.Extensions["correlationId"] = correlationId;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}
