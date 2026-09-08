using System.ComponentModel.DataAnnotations;
using TravelManager.Models;

namespace TravelManager.DTOs
{
    public class CreateBookingDto
    {
        [Required]
        public int TourId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        [Range(1, 20, ErrorMessage = "Кількість дорослих від 1 до 20")]
        public int NumberOfAdults { get; set; }

        [Range(0, 20, ErrorMessage = "Кількість дітей від 0 до 20")]
        public int NumberOfChildren { get; set; }

        [MaxLength(500, ErrorMessage = "Коментар не може перевищувати 500 символів")]
        public string? Comment { get; set; }

        [Required]
        public List<BookingPersonDto> Persons { get; set; } = new();
    }
}
