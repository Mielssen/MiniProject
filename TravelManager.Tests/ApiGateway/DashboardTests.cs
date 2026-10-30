using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace TravelManager.Tests.ApiGateway;

public class DashboardTests
{
    private const string JwtKey =
        "TestKeyForApiGatewayIntegrationTests123456789";

    private static HttpClient CreateAuthenticatedClient(
        ApiGatewayFactory factory,
        int userId = 1)
    {
        var client = factory.CreateClient();

        var token = GenerateToken(userId);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static string GenerateToken(int userId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(JwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "TravelManager",
            audience: "TravelManagerUsers",
            claims:
            [
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()),
                new Claim(ClaimTypes.Role, "client")
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string content)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                content,
                Encoding.UTF8,
                "application/json")
        };
    }

    private static bool IsCatalogRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.AbsolutePath.Contains(
            "catalog",
            StringComparison.OrdinalIgnoreCase) == true;
    }

    // 1. Dashboard без авторизації повертає 401.

    [Fact]
    public async Task GetDashboard_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new ApiGatewayFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 2. Dashboard з неправильним JWT повертає 401.

    [Fact]
    public async Task GetDashboard_WithInvalidToken_ReturnsUnauthorized()
    {
        using var factory = new ApiGatewayFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 3. Dashboard повертає повну відповідь.

    [Fact]
    public async Task GetDashboard_WithValidToken_ReturnsCompleteData()
    {
        using var factory = new ApiGatewayFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("catalog").ValueKind == JsonValueKind.Object);
        Assert.True(json.GetProperty("bookings").ValueKind == JsonValueKind.Object);
        Assert.False(json.GetProperty("partial").GetBoolean());
        Assert.Equal(JsonValueKind.Null,
            json.GetProperty("errors").GetProperty("catalog").ValueKind);
        Assert.Equal(JsonValueKind.Null,
            json.GetProperty("errors").GetProperty("bookings").ValueKind);
    }

    // 4. Dashboard повертає часткову відповідь, якщо Catalog недоступний.

    [Fact]
    public async Task GetDashboard_WhenCatalogUnavailable_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (request, _) =>
            Task.FromResult(IsCatalogRequest(request)
                ? CreateResponse(HttpStatusCode.ServiceUnavailable, "{}")
                : CreateResponse(HttpStatusCode.OK, """{"bookings": []}"""));

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.NotNull(json.GetProperty("errors").GetProperty("catalog").GetString());
        Assert.Equal(JsonValueKind.Object,
            json.GetProperty("bookings").ValueKind);
    }

    // 5. Dashboard повертає часткову відповідь, якщо Booking недоступний.

    [Fact]
    public async Task GetDashboard_WhenBookingUnavailable_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (request, _) =>
            Task.FromResult(IsCatalogRequest(request)
                ? CreateResponse(HttpStatusCode.OK, """{"items": []}""")
                : CreateResponse(HttpStatusCode.ServiceUnavailable, "{}"));

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.NotNull(json.GetProperty("errors").GetProperty("bookings").GetString());
    }

    // 6. Dashboard повертає часткову відповідь, якщо обидва сервіси недоступні.

    [Fact]
    public async Task GetDashboard_WhenBothServicesUnavailable_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (_, _) =>
            Task.FromResult(
                CreateResponse(HttpStatusCode.ServiceUnavailable, "{}"));

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.NotNull(json.GetProperty("errors").GetProperty("catalog").GetString());
        Assert.NotNull(json.GetProperty("errors").GetProperty("bookings").GetString());
    }

    // 7. Dashboard обробляє некоректний JSON від Catalog.

    [Fact]
    public async Task GetDashboard_WhenCatalogReturnsInvalidJson_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (request, _) =>
            Task.FromResult(CreateResponse(
                HttpStatusCode.OK,
                IsCatalogRequest(request) ? "{ invalid json" : """{"bookings": []}"""));

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.Contains(
            "JSON",
            json.GetProperty("errors").GetProperty("catalog").GetString());
    }

    // 8. Dashboard обробляє некоректний JSON від Booking.

    [Fact]
    public async Task GetDashboard_WhenBookingReturnsInvalidJson_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (request, _) =>
            Task.FromResult(CreateResponse(
                HttpStatusCode.OK,
                IsCatalogRequest(request) ? """{"items": []}""" : "{ invalid json"));

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.Contains(
            "JSON",
            json.GetProperty("errors").GetProperty("bookings").GetString());
    }

    // 9. Dashboard обробляє таймаут Catalog.

    [Fact]
    public async Task GetDashboard_WhenCatalogTimesOut_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = async (request, token) =>
        {
            if (IsCatalogRequest(request))
                await Task.Delay(TimeSpan.FromSeconds(5), token);

            return CreateResponse(HttpStatusCode.OK, """{"data": []}""");
        };

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.NotNull(json.GetProperty("errors").GetProperty("catalog").GetString());
    }

    // 10. Dashboard обробляє таймаут Booking.

    [Fact]
    public async Task GetDashboard_WhenBookingTimesOut_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = async (request, token) =>
        {
            if (!IsCatalogRequest(request))
                await Task.Delay(TimeSpan.FromSeconds(5), token);

            return CreateResponse(HttpStatusCode.OK, """{"data": []}""");
        };

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.NotNull(json.GetProperty("errors").GetProperty("bookings").GetString());
    }

    // 11. Dashboard обробляє помилку з'єднання з Catalog.

    [Fact]
    public async Task GetDashboard_WhenCatalogConnectionFails_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (request, _) =>
        {
            if (IsCatalogRequest(request))
                throw new HttpRequestException("Catalog connection failed");

            return Task.FromResult(
                CreateResponse(HttpStatusCode.OK, """{"bookings": []}"""));
        };

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.NotNull(json.GetProperty("errors").GetProperty("catalog").GetString());
    }

    // 12. Dashboard обробляє помилку з'єднання з Booking.

    [Fact]
    public async Task GetDashboard_WhenBookingConnectionFails_ReturnsPartialResponse()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (request, _) =>
        {
            if (!IsCatalogRequest(request))
                throw new HttpRequestException("Booking connection failed");

            return Task.FromResult(
                CreateResponse(HttpStatusCode.OK, """{"items": []}"""));
        };

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("partial").GetBoolean());
        Assert.NotNull(json.GetProperty("errors").GetProperty("bookings").GetString());
    }

    // 13. Dashboard передає JWT до BookingService.

    [Fact]
    public async Task GetDashboard_ForwardsAuthorizationHeaderToBooking()
    {
        using var factory = new ApiGatewayFactory();

        var authorizationHeaders = new ConcurrentDictionary<string, string>();

        factory.RequestHandler = (request, _) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            authorizationHeaders[path] =
                request.Headers.Authorization?.ToString() ?? "";

            return Task.FromResult(
                CreateResponse(HttpStatusCode.OK, "{}"));
        };

        using var client = CreateAuthenticatedClient(factory);

        var expectedToken = client.DefaultRequestHeaders.Authorization!.ToString();

        await client.GetAsync("/api/dashboard");

        var bookingHeader = authorizationHeaders
            .First(x => x.Key.Contains("bookings")).Value;

        Assert.Equal(expectedToken, bookingHeader);
    }

    // 14. Dashboard не передає JWT до CatalogService.

    [Fact]
    public async Task GetDashboard_DoesNotForwardAuthorizationToCatalog()
    {
        using var factory = new ApiGatewayFactory();

        string? catalogAuthorization = null;

        factory.RequestHandler = (request, _) =>
        {
            if (IsCatalogRequest(request))
            {
                catalogAuthorization =
                    request.Headers.Authorization?.ToString();
            }

            return Task.FromResult(
                CreateResponse(HttpStatusCode.OK, "{}"));
        };

        using var client = CreateAuthenticatedClient(factory);

        await client.GetAsync("/api/dashboard");

        Assert.Null(catalogAuthorization);
    }

    // 15. Dashboard передає Correlation ID обом сервісам.

    [Fact]
    public async Task GetDashboard_ForwardsCorrelationIdToBothServices()
    {
        using var factory = new ApiGatewayFactory();

        var correlationIds = new ConcurrentDictionary<string, string>();

        factory.RequestHandler = (request, _) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            correlationIds[path] =
                request.Headers.TryGetValues(
                    "X-Correlation-Id",
                    out var values)
                    ? values.First()
                    : "";

            return Task.FromResult(
                CreateResponse(HttpStatusCode.OK, "{}"));
        };

        using var client = CreateAuthenticatedClient(factory);

        const string expectedId = "test-correlation-id";

        client.DefaultRequestHeaders.Add(
            "X-Correlation-Id",
            expectedId);

        await client.GetAsync("/api/dashboard");

        Assert.All(
            correlationIds.Values,
            value => Assert.Equal(expectedId, value));

        Assert.Equal(2, correlationIds.Count);
    }

    // 16. Dashboard зберігає вкладені JSON-об'єкти та масиви.

    [Fact]
    public async Task GetDashboard_PreservesNestedJsonStructures()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (request, _) =>
            Task.FromResult(CreateResponse(
                HttpStatusCode.OK,
                IsCatalogRequest(request)
                    ? """
                      {
                          "items": [
                              {
                                  "id": 1,
                                  "details": {
                                      "country": "Ukraine",
                                      "tags": ["sea", "mountains"]
                                  }
                              }
                          ]
                      }
                      """
                    : """{"bookings": []}"""));

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var item = json.GetProperty("catalog")
            .GetProperty("items")[0];

        Assert.Equal(
            "Ukraine",
            item.GetProperty("details").GetProperty("country").GetString());

        Assert.Equal(
            2,
            item.GetProperty("details").GetProperty("tags").GetArrayLength());
    }

    // 17. Dashboard зберігає числові типи JSON.

    [Fact]
    public async Task GetDashboard_PreservesNumericJsonValues()
    {
        using var factory = new ApiGatewayFactory();

        factory.RequestHandler = (request, _) =>
            Task.FromResult(CreateResponse(
                HttpStatusCode.OK,
                IsCatalogRequest(request)
                    ? """{"count": 15, "price": 1250.50}"""
                    : """{"count": 3}"""));

        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/dashboard");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var catalog = json.GetProperty("catalog");

        Assert.Equal(15, catalog.GetProperty("count").GetInt32());
        Assert.Equal(1250.50m, catalog.GetProperty("price").GetDecimal());
    }

    // 18. Gateway повертає 429 та Retry-After після перевищення ліміту.

    [Fact]
    public async Task GetDashboard_WhenRateLimitExceeded_Returns429AndRetryAfter()
    {
        using var factory = new ApiGatewayFactory();
        using var client = CreateAuthenticatedClient(factory);

        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 31; i++)
        {
            lastResponse?.Dispose();

            lastResponse = await client.GetAsync("/api/dashboard");
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            lastResponse.StatusCode);

        Assert.True(
            lastResponse.Headers.Contains("Retry-After"));

        Assert.Equal(
            "60",
            lastResponse.Headers.GetValues("Retry-After").First());

        lastResponse.Dispose();
    }
}