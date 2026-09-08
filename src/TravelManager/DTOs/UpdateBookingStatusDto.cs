using System.ComponentModel.DataAnnotations;

namespace TravelManager.DTOs
{
    public class UpdateBookingStatusDto
    {
        [Required]
        [RegularExpression("^(pending|confirmed|cancelled)$",
            ErrorMessage = "Статус має бути pending, confirmed або cancelled")]
        public string Status { get; set; }
    }
}