namespace TravelManager.DTOs
{
    public class BookingPersonDto
    {
        public string Name { get; set; } = null!;

        public string? PassportData { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public bool IsChild { get; set; }
    }
}
