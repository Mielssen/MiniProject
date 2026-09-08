using System.ComponentModel.DataAnnotations;

namespace TravelManager.DTOs
{
    public class UpdateUserStatusDto
    {
        [Required]
        [RegularExpression("^(active|deleted)$",
        ErrorMessage = "Статус має бути active або deleted")]
        public string Status { get; set; }
    }
}
