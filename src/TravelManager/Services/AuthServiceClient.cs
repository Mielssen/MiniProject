using System.Net.Http.Headers;
using System.Net.Http.Json;
using TravelManager.DTOs;

namespace TravelManager.Services;

public class AuthServiceClient
{
    private readonly HttpClient _httpClient;

    public AuthServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<AuthUserDto?> GetUserByIdAsync(
        int userId,
        string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/users/{userId}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthUserDto>();
    }

}