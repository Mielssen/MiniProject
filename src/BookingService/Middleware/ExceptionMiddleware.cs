using BookingService.Exceptions;

namespace BookingService.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
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
        catch (AppException ex)
        {
            _logger.LogWarning(
                "AppException: Code={Code}, Message={Message}",
                ex.Code,
                ex.Message);

            context.Response.StatusCode = 400;

            await context.Response.WriteAsJsonAsync(new
            {
                Message = ex.Message,
                Code = ex.Code
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необроблена помилка сервера");

            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = 500;

            await context.Response.WriteAsJsonAsync(new
            {
                Message = "Internal server error"
            });
        }
    }
}