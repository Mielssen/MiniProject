using System.ComponentModel.DataAnnotations;

namespace TravelManager.DTOs
{
    public class TourAssetDto
    {
        [Required]
        public string AssetType { get; set; }

        [Required]
        public string Url { get; set; }
    }
}
