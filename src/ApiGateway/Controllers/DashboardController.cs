using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiGateway.Models;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
/// <summary>
/// Отримує зведену інформацію для панелі користувача.
/// </summary>
/// <remarks>
/// Паралельно отримує список доступних турів із CatalogService
/// та особисті бронювання користувача з BookingService.
/// Якщо один із сервісів недоступний, повертає часткову відповідь.
/// </remarks>
public class DashboardController : ControllerBase
{
    private const string CatalogUrl =
        "http://localhost:5002/api/v1/catalog/items";

    private const string BookingsUrl =
        "http://localhost:5100/api/bookings/my";

    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    /// <summary>
    /// Отримання даних Dashboard.
    /// </summary>
    /// <remarks>
    /// Приклад повної відповіді:
    ///
    ///     {
    ///       "catalog": [],
    ///       "bookings": [],
    ///       "partial": false,
    ///       "errors": {
    ///         "catalog": null,
    ///         "bookings": null
    ///       }
    ///     }
    ///
    /// Приклад часткової відповіді:
    ///
    ///     {
    ///       "catalog": [],
    ///       "bookings": null,
    ///       "partial": true,
    ///       "errors": {
    ///         "catalog": null,
    ///         "bookings": "Сервіс недоступний"
    ///       }
    ///     }
    /// </remarks>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> GetDashboard(
        CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();
        var correlationId = Request.Headers["X-Correlation-Id"].ToString();

        var catalogTask = GetServiceDataAsync(
            CatalogUrl, null, correlationId, cancellationToken);

        var bookingsTask = GetServiceDataAsync(
            BookingsUrl, authorization, correlationId, cancellationToken);

        var results = await Task.WhenAll(catalogTask, bookingsTask);

        var catalog = results[0];
        var bookings = results[1];

        return Ok(new DashboardResponse
        {
            Catalog = catalog.Data,
            Bookings = bookings.Data,
            Partial = catalog.Error != null || bookings.Error != null,
            Errors = new DashboardErrors
            {
                Catalog = catalog.Error,
                Bookings = bookings.Error
            }
        });
    }

    private async Task<ServiceResult> GetServiceDataAsync(
    string url,
    string? authorization,
    string correlationId,
    CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrWhiteSpace(authorization))
            {
                request.Headers.TryAddWithoutValidation(
                    "Authorization", authorization);
            }

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                request.Headers.TryAddWithoutValidation(
                    "X-Correlation-Id", correlationId);
            }

            var client = _httpClientFactory.CreateClient("Dashboard");

            using var response = await client.SendAsync(
                request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ServiceResult(
                    null,
                    $"Сервіс повернув HTTP {(int)response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(
                cancellationToken);

            using var document = JsonDocument.Parse(content);

            var data = ConvertJsonElement(document.RootElement);

            return new ServiceResult(data, null);
        }
        catch (HttpRequestException)
        {
            return new ServiceResult(null, "Сервіс недоступний");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return new ServiceResult(null, "Сервіс не відповів вчасно");
        }
        catch (JsonException)
        {
            return new ServiceResult(null, "Сервіс повернув некоректний JSON");
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return element.EnumerateObject().ToDictionary(
                    property => property.Name,
                    property => ConvertJsonElement(property.Value));

            case JsonValueKind.Array:
                return element.EnumerateArray()
                    .Select(ConvertJsonElement)
                    .ToList();

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var integer))
                    return integer;

                if (element.TryGetDecimal(out var decimalValue))
                    return decimalValue;

                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;

            default:
                return null;
        }
    }

    private sealed record ServiceResult(
        object? Data,
        string? Error);
}
