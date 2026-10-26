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

    public async Task<AuthUserDto?> GetUserByIdAsync(int userId, string token)
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

    public async Task<List<AuthUserDto>> GetAllUsersAsync(string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/v1/users");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<AuthUserDto>>()
            ?? new List<AuthUserDto>();
    }

    public async Task UpdateRoleAsync(int userId, string role, string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/v1/users/{userId}/roles");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(new
        {
            Roles = new[] { role }
        });

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateStatusAsync(int userId, bool isActive, string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/v1/users/{userId}/status");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(new { IsActive = isActive });

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateUserDataAsync(
        int userId,
        UpdateProfileDto dto,
        string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/v1/users/{userId}/data");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(dto);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
