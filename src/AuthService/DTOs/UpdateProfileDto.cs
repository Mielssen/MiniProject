using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class UpdateProfileDto
{
    /// <summary>
    /// Нове ім'я користувача.
    /// </summary>
    [Required]
    [RegularExpression(
        @"^[a-zA-Zа-яА-ЯіІїЇєЄ]+$",
        ErrorMessage = "Ім'я має містити тільки літери")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Новий номер телефону користувача.
    /// </summary>
    [RegularExpression(
        @"^\d{10}$",
        ErrorMessage = "Телефон має містити рівно 10 цифр")]
    public string? Phone { get; set; }

    /// <summary>
    /// Нові паспортні дані користувача.
    /// </summary>
    public string? PassportData { get; set; }

    /// <summary>
    /// Нова дата народження користувача.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
}