using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class LoginDto
{
    /// <summary>
    /// Email користувача.
    /// </summary>
    [Required(ErrorMessage = "Email обов'язковий")]
    [EmailAddress(ErrorMessage = "Неправильний формат пошти")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Пароль користувача.
    /// </summary>
    [Required(ErrorMessage = "Пароль обов'язковий")]
    public string Password { get; set; } = null!;
}