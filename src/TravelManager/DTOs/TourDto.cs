namespace TravelManager.DTOs
{
    public class TourDto
    {
        public int TourId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableTo { get; set; }
        public int TypeId { get; set; }
        public List<TourAssetDto> Assets { get; set; }
    }
}
