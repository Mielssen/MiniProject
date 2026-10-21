namespace AuthService.DTOs;

public class TokenResponceDto
{
    /// <summary>
    /// JWT access token для авторизації запитів.
    /// </summary>
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// Refresh token для отримання нового access token.
    /// </summary>
    public string RefreshToken { get; set; } = null!;

    /// <summary>
    /// Дата та час завершення дії access token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}