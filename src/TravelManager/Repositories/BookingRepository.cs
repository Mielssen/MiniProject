using Microsoft.EntityFrameworkCore;
using TravelManager.Models;

namespace TravelManager.Repositories
{
    public class BookingRepository
    {
        private readonly TravelmanagerContext _context;

        public BookingRepository(TravelmanagerContext context)
        {
            _context = context;
        }

        public void Add(Booking booking)
        {
            _context.Bookings.Add(booking);
            _context.SaveChanges();
        }
        public bool TourExists(int tourId)
        {
            return _context.Tours.Any(t => t.TourId == tourId);
        }

        public List<Booking> GetByCredentialId(int credentialId)
        {
            return _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.BookingPeople)
                .Include(b => b.Credential)
                    .ThenInclude(c => c.UserDetail)
                .Where(b => b.CredentialId == credentialId)
                .ToList();
        }
        public List<Booking> GetAll(string? status)
        {
            var query = _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.BookingPeople)
                .Include(b => b.Credential)
                    .ThenInclude(c => c.UserDetail)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(b => b.Status == status);

            return query.ToList();
        }
        public void Update(Booking booking)
        {
            _context.Bookings.Update(booking);
            _context.SaveChanges();
        }
        public Booking? GetById(int id)
        {
            return _context.Bookings
                .Include(b => b.Tour)
                .FirstOrDefault(b=> b.BookingId == id);
        }
        public Tour? GetTourById(int tourId)
        {
            return _context.Tours.FirstOrDefault(t => t.TourId == tourId);
        }

        public void Delete(Booking booking)
        {
            _context.Bookings.Remove(booking);
            _context.SaveChanges();
        }

    }
}
