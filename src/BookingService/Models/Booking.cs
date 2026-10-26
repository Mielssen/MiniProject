namespace BookingService.Models;

public class Booking
{
    public int BookingId { get; set; }
    public int? UserId { get; set; }
    public int TourId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateTime? BookingDate { get; set; }
    public string Status { get; set; } = "pending";
    public int NumberOfPeople { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Comment { get; set; }

    public ICollection<BookingPerson> Persons { get; set; } = new List<BookingPerson>();
}