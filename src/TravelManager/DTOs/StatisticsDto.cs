namespace TravelManager.DTOs
{
    public class StatisticsDto
    {
        public int TotalUsers { get; set; }
        public int TotalManagers { get; set; }
        public int TotalClients { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ConfirmedRevenue { get; set; }
        public List<TopTourDto> TopTours { get; set; } = new();
        public int NewUsersThisMonth { get; set; }
        public int NewBookingsThisMonth { get; set; }
    }

    public class TopTourDto
    {
        public int TourId { get; set; }
        public string Name { get; set; }
        public int BookingsCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}