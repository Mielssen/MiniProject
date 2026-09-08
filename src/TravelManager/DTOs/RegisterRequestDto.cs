using System.ComponentModel.DataAnnotations;

namespace TravelManager.DTOs
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Неправильний формат пошти")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обов'язковий")]
        [MinLength(12, ErrorMessage = "Пароль має містити не менше 12 символів")]
        [MaxLength(48, ErrorMessage = "Пароль має містити не більше 48 символів")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Пароль має містити великі, малі літери і цифри")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Дата народження обов'язкова")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Ім'я обов'язкове")]
        [RegularExpression(@"^[a-zA-Zа-яА-ЯіІїЇєЄ]+$",
            ErrorMessage = "Ім'я має містити тільки літери")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Телефон обов'язковий")]
        [RegularExpression(@"^\d{10}$",
            ErrorMessage = "Телефон має містити рівно 10 цифр")]
        public string Phone { get; set; }

        public string PassportData { get; set; }
    }
}