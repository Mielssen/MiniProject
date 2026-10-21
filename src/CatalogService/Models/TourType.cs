namespace CatalogService.Models;

public class TourType
{
    public int TypeId { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<Tour> Tours { get; set; } = new List<Tour>();
}