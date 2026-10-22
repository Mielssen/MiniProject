
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TravelManager.DTOs;

namespace TravelManager.Services;

public class BookingServiceClient
{
    private readonly HttpClient _httpClient;

    public BookingServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BookingStatisticsDto>> GetAllBookingsAsync(string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/bookings");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<BookingStatisticsDto>>()
            ?? new List<BookingStatisticsDto>();
    }
}

public class BookingStatisticsDto
{
    public int BookingId { get; set; }
    public int TourId { get; set; }
    public string TourName { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public DateTime BookingDate { get; set; }
}
