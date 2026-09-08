using System.Security.Claims;

namespace TravelManager.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var start = DateTime.UtcNow;

            await _next(context);

            var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
            var user = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

            _logger.LogInformation(
                "{Method} {Path} | Status={StatusCode} | User={User} | {Elapsed}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                user,
                elapsed);
        }
    }
}
