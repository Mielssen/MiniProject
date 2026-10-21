namespace CatalogService.Models;

public class Tour
{
    public int TourId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public DateTime AvailableFrom { get; set; }

    public DateTime AvailableTo { get; set; }

    public int TypeId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TourType Type { get; set; } = null!;

    public ICollection<TourAsset> TourAssets { get; set; } = new List<TourAsset>();
}