using System;
using System.Collections.Generic;

namespace TravelManager.Models;

public partial class BookingPerson
{
    public int PersonId { get; set; }

    public int BookingId { get; set; }

    public string Name { get; set; } = null!;

    public string? PassportData { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool IsChild { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
