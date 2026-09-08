using System;
using System.Collections.Generic;

namespace TravelManager.Models;

public partial class Tour
{
    public int TourId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public DateTime AvailableFrom { get; set; }

    public DateTime AvailableTo { get; set; }

    public int TypeId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<TourAsset> TourAssets { get; set; } = new List<TourAsset>();

    public virtual TourType Type { get; set; } = null!;
}
