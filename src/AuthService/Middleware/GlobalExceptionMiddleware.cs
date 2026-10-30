using System.Text.Json;
using AuthService.Exceptions;

namespace AuthService.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
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
                "Application error. Code={Code}",
                ex.Code);

            var statusCode = ex.Code switch
            {
                "EMAIL_EXISTS" => StatusCodes.Status409Conflict,
                "NOT_FOUND" => StatusCodes.Status404NotFound,
                "UNDERAGE" => StatusCodes.Status400BadRequest,
                "INVALID_EMAIL" => StatusCodes.Status400BadRequest,
                "INVALID_REFRESH_TOKEN" => StatusCodes.Status401Unauthorized,
                "WRONG_PASSWORD" => StatusCodes.Status401Unauthorized,
                "ACCOUNT_DISABLED" => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var response = new
            {
                type = $"https://httpstatuses.com/{statusCode}",
                title = ex.Code,
                status = statusCode,
                detail = ex.Message
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Непередбачена помилка");

            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            context.Response.ContentType =
                "application/problem+json";

            var response = new
            {
                type = "https://httpstatuses.com/500",
                title = "Internal Server Error",
                status = 500,
                detail = "Внутрішня помилка сервера"
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}