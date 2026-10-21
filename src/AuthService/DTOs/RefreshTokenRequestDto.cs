using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RefreshTokenRequestDto
{
    /// <summary>
    /// Refresh token, отриманий під час реєстрації або авторизації.
    /// </summary>
    [Required(ErrorMessage = "Refresh token обов'язковий")]
    public string RefreshToken { get; set; } = null!;
}