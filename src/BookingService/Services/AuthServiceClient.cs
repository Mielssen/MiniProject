using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingService.DTOs;

namespace BookingService.Services;

public class AuthServiceClient
{
    private readonly HttpClient _httpClient;

    public AuthServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthUserDto?> GetUserByIdAsync(
        int userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/users/{userId}");

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            token);
        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthUserDto>(
            cancellationToken: cancellationToken);
    }
}