using System;
using System.Collections.Generic;

namespace TravelManager.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int CredentialId { get; set; }

    public int TourId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateTime? BookingDate { get; set; }

    public string Status { get; set; } = null!;

    public int NumberOfPeople { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Comment { get; set; }

    public virtual ICollection<BookingPerson> BookingPeople { get; set; } = new List<BookingPerson>();

    public virtual Credential Credential { get; set; } = null!;

    public virtual ICollection<Penalty> Penalties { get; set; } = new List<Penalty>();

    public virtual Tour Tour { get; set; } = null!;

}
