using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookingService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TravelManager.Tests.BookingService;

public class BookingServiceTests
{
    private static async Task InitializeDatabaseAsync(
        BookingServiceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    private static void Authenticate(
        HttpClient client,
        int userId = 1,
        string role = "client")
    {
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
    }

    private static object ValidBookingRequest() => new
    {
        tourId = 1,
        startDate = "2030-06-01T00:00:00",
        numberOfAdults = 1,
        numberOfChildren = 0,
        comment = "Тестове бронювання",
        persons = new[]
        {
            new
            {
                name = "Іван",
                passportData = "123456",
                dateOfBirth = "2000-01-01",
                isChild = false
            }
        }
    };

    [Fact]
    public async Task GetMy_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = new BookingServiceFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/bookings/my");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsClient_ReturnsForbidden()
    {
        using var factory = new BookingServiceFactory();
        using var client = factory.CreateClient();
        await InitializeDatabaseAsync(factory);
        Authenticate(client);

        var response = await client.GetAsync("/api/bookings");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_AsClient_ReturnsForbidden()
    {
        using var factory = new BookingServiceFactory();
        using var client = factory.CreateClient();
        await InitializeDatabaseAsync(factory);
        Authenticate(client);

        var response = await client.PutAsJsonAsync(
            "/api/bookings/1/status",
            new { status = "confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsManager_ReturnsEmptyList()
    {
        using var factory = new BookingServiceFactory();
        using var client = factory.CreateClient();
        await InitializeDatabaseAsync(factory);
        Authenticate(client, role: "manager");

        var response = await client.GetAsync("/api/bookings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bookings =
            await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        using var factory = new BookingServiceFactory();
        using var client = factory.CreateClient();
        await InitializeDatabaseAsync(factory);
        Authenticate(client, role: "manager");

        var response = await client.PutAsJsonAsync(
            "/api/bookings/1/status",
            new { status = "invalid-status" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        using var factory = new BookingServiceFactory();
        using var client = factory.CreateClient();
        await InitializeDatabaseAsync(factory);
        Authenticate(client);

        var response = await client.PostAsJsonAsync(
            "/api/bookings",
            ValidBookingRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithIdempotencyKeyOver100Characters_ReturnsBadRequest()
    {
        using var factory = new BookingServiceFactory();
        using var client = factory.CreateClient();
        await InitializeDatabaseAsync(factory);
        Authenticate(client);

        client.DefaultRequestHeaders.Add(
            "Idempotency-Key",
            new string('a', 101));

        var response = await client.PostAsJsonAsync(
            "/api/bookings",
            ValidBookingRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ForMissingBooking_ReturnsBadRequest()
    {
        using var factory = new BookingServiceFactory();
        using var client = factory.CreateClient();

        await InitializeDatabaseAsync(factory);
        Authenticate(client, role: "manager");

        var response = await client.PutAsJsonAsync(
            "/api/bookings/999/status",
            new { status = "confirmed" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
