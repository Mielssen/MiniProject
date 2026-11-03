using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs;

public class CreateCatalogItemDto
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0.01, 100000000)]
    public decimal Price { get; set; }

    [Range(1, 365)]
    public int DurationDays { get; set; }

    [Required]
    public DateTime AvailableFrom { get; set; }

    [Required]
    public DateTime AvailableTo { get; set; }

    [Range(1, int.MaxValue)]
    public int TypeId { get; set; }
}