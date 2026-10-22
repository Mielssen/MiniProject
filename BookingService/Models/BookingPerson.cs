namespace BookingService.Models;

public class BookingPerson
{
    public int PersonId { get; set; }
    public int BookingId { get; set; }
    public string Name { get; set; } = null!;
    public string? PassportData { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsChild { get; set; }

    public Booking Booking { get; set; } = null!;
}