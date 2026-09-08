using System.ComponentModel.DataAnnotations;

namespace TravelManager.DTOs
{
    public class CreateTourDto
    {
        [Required(ErrorMessage = "Назва туру обов'язкова")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Ціна обов'язкова")]
        [Range(0, 1000000, ErrorMessage = "Некоректна ціна")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Тривалість обов'язкова")]
        [Range(1, 365, ErrorMessage = "Тривалість має бути від 1 до 365 днів")]
        public int DurationDays { get; set; }

        [Required(ErrorMessage = "Дата початку обов'язкова")]
        public DateTime AvailableFrom { get; set; }

        [Required(ErrorMessage = "Дата завершення обов'язкова")]
        public DateTime AvailableTo { get; set; }

        [Required(ErrorMessage = "Тип туру обов'язковий")]
        public int TypeId { get; set; }

        public List<TourAssetDto>? Assets { get; set; }
    }
}
