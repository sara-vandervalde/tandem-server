using System.Diagnostics;

namespace Tandem.API.Middleware;

// Gives every request an id that appears in all of its log lines and in the response header,
// so one request can be traced through a shared log file
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private const int MaxIncomingIdLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Read by ExceptionHandlingMiddleware, which runs outside this logging scope
        context.Items[ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Every log line written during this request (in any layer) gets the CorrelationId
        using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            _logger.LogInformation("{Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }

    // A client may send its own id (useful for tracing across systems); otherwise a new one is created.
    // The incoming value is length-limited because it is written to the log
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();

        return !string.IsNullOrWhiteSpace(incoming) && incoming.Length <= MaxIncomingIdLength
            ? incoming
            : Guid.NewGuid().ToString();
    }
}
