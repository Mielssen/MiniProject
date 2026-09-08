using System;
using System.Collections.Generic;

namespace TravelManager.Models;

public partial class TourAsset
{
    public int AssetId { get; set; }

    public int TourId { get; set; }

    public string AssetType { get; set; } = null!;

    public string Url { get; set; } = null!;

    public virtual Tour Tour { get; set; } = null!;
}
