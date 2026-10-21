namespace CatalogService.Models;

public class TourAsset
{
    public int AssetId { get; set; }

    public int TourId { get; set; }

    public string AssetType { get; set; } = null!;

    public string Url { get; set; } = null!;

    public Tour Tour { get; set; } = null!;
}