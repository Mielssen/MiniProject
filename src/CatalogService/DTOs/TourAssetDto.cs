namespace CatalogService.DTOs;

public class TourAssetDto
{
    public int Id { get; set; }

    public string AssetType { get; set; } = null!;

    public string Url { get; set; } = null!;
}