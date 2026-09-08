namespace TravelManager.DTOs
{
    public class BookingDto
    {
        public int BookingId { get; set; }
        public int TourId { get; set; }
        public string TourName { get; set; }
        public decimal TourPrice { get; set; }
        public int TourDurationDays { get; set; }

        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public string? UserPassportData { get; set; }
        public DateTime? UserDateOfBirth { get; set; }

        public DateOnly StartDate { get; set; }
        public DateTime? BookingDate { get; set; }
        public string Status { get; set; }
        public int NumberOfPeople { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Comment { get; set; }
        public List<BookingPersonDto> Persons { get; set; } = new();
    }
}