using TravelManager.Models;
using Microsoft.EntityFrameworkCore;

namespace TravelManager.Repositories
{
    public class AdminRepository
    {
        private readonly TravelmanagerContext _context;

        public AdminRepository(TravelmanagerContext context)
        {
            _context = context;
        }

        public List<Credential> GetAllUsers(string? role, string? status)
        {
            var query = _context.Credentials
                .Include(c => c.UserDetail)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(c => c.Role == role);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            return query.OrderBy(c => c.CreatedAt).ToList();
        }

        public Credential? GetUserById(int credentialId)
        {
            return _context.Credentials
                .Include(c => c.UserDetail)
                .FirstOrDefault(c => c.CredentialId == credentialId);
        }

        public void UpdateCredential(Credential credential)
        {
            _context.Credentials.Update(credential);
            _context.SaveChanges();
        }

        public void UpdateUserDetail(UserDetail detail)
        {
            _context.UserDetails.Update(detail);
            _context.SaveChanges();
        }

        public int CountByRole(string role) =>
            _context.Credentials.Count(c => c.Role == role && c.Status == "active");

        public int CountBookingsByStatus(string status) =>
            _context.Bookings.Count(b => b.Status == status);

        public int CountAllBookings() =>
            _context.Bookings.Count();

        public decimal SumRevenue() =>
            _context.Bookings.Sum(b => (decimal?)b.TotalPrice) ?? 0;

        public decimal SumConfirmedRevenue() =>
            _context.Bookings
                .Where(b => b.Status == "confirmed")
                .Sum(b => (decimal?)b.TotalPrice) ?? 0;

        public List<(int TourId, string Name, int Count, decimal Revenue)> GetTopTours(int top)
        {
            return _context.Bookings
                .Include(b => b.Tour)
                .Where(b => b.Status != "cancelled")
                .GroupBy(b => new { b.TourId, b.Tour.Name })
                .Select(g => new
                {
                    g.Key.TourId,
                    g.Key.Name,
                    Count = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .AsEnumerable()
                .Select(x => (x.TourId, x.Name, x.Count, x.Revenue))
                .ToList();
        }

        public int CountNewUsersThisMonth()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _context.Credentials.Count(c => c.CreatedAt >= start);
        }

        public int CountNewBookingsThisMonth()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _context.Bookings.Count(b => b.BookingDate >= start);
        }
    }
}
