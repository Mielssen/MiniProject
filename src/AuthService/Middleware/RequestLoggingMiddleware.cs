namespace AuthService.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var correlationId = context.Request.Headers["X-Correlation-Id"]
            .FirstOrDefault() ?? "not-provided";

        _logger.LogInformation(
            "Request completed. Method={Method}, StatusCode={StatusCode}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
            context.Request.Method,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            correlationId);
    }
}
