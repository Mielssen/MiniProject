namespace CatalogService.DTOs;

public class CatalogItemDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public DateTime AvailableFrom { get; set; }

    public DateTime AvailableTo { get; set; }

    public int TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public List<TourAssetDto> Assets { get; set; } = new();
}