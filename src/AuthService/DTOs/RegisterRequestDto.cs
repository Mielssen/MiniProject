using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RegisterRequestDto
{
    /// <summary>
    /// Email нового користувача.
    /// </summary>
    [Required(ErrorMessage = "Email обов'язковий")]
    [EmailAddress(ErrorMessage = "Неправильний формат пошти")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Пароль користувача. Від 12 до 48 символів, має містити великі та малі літери і цифри.
    /// </summary>
    [Required(ErrorMessage = "Пароль обов'язковий")]
    [MinLength(12, ErrorMessage = "Пароль має містити не менше 12 символів")]
    [MaxLength(48, ErrorMessage = "Пароль має містити не більше 48 символів")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Пароль має містити великі, малі літери і цифри")]
    public string Password { get; set; } = null!;

    /// <summary>
    /// Дата народження користувача.
    /// </summary>
    [Required(ErrorMessage = "Дата народження обов'язкова")]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Ім'я користувача.
    /// </summary>
    [Required(ErrorMessage = "Ім'я обов'язкове")]
    [RegularExpression(
        @"^[a-zA-Zа-яА-ЯіІїЇєЄ]+$",
        ErrorMessage = "Ім'я має містити тільки літери")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Номер телефону користувача у форматі 10 цифр.
    /// </summary>
    [Required(ErrorMessage = "Телефон обов'язковий")]
    [RegularExpression(
        @"^\d{10}$",
        ErrorMessage = "Телефон має містити рівно 10 цифр")]
    public string Phone { get; set; } = null!;

    /// <summary>
    /// Паспортні дані користувача.
    /// </summary>
    public string? PassportData { get; set; }
}