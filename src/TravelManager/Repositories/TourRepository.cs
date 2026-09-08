using Microsoft.EntityFrameworkCore;
using TravelManager.Models;

namespace TravelManager.Repositories
{
    public class TourRepository
    {
        private readonly TravelmanagerContext _context;

        public TourRepository(TravelmanagerContext context)
        {
            _context = context;
        }

        public void Add(Tour tour)
        {
            _context.Tours.Add(tour);
            _context.SaveChanges();
        }
        public List<Tour> Search(string query)
        {
            return _context.Tours
                .Include(t => t.TourAssets)
                .Where(t => t.Name.Contains(query) || t.Description.Contains(query))
                .ToList();
        }
        public List<Tour> GetAll()
        {
            return _context.Tours
                .Include(t => t.TourAssets)
                .ToList();
        }

        public void Update(Tour tour)
        {
            _context.Tours.Update(tour);
            _context.SaveChanges();
        }

        public Tour GetById(int id)
        {
            return _context.Tours
                .Include(x => x.TourAssets)
                .FirstOrDefault(x => x.TourId == id);
        }

        public void Delete(Tour tour)
        {
            _context.Tours.Remove(tour);
            _context.SaveChanges();
        }
    }
}
