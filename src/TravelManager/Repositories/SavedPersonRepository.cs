using TravelManager.Models;

namespace TravelManager.Repositories
{
    public class SavedPersonRepository
    {
        private readonly TravelmanagerContext _context;

        public SavedPersonRepository(TravelmanagerContext context)
        {
            _context = context;
        }

        public List<SavedPerson> GetByUserId(int userId)
        {
            return _context.SavedPersons
                .Where(p => p.UserId == userId)
                .ToList();
        }

        public void Add(SavedPerson person)
        {
            _context.SavedPersons.Add(person);
            _context.SaveChanges();
        }

        public SavedPerson? GetById(int id)
        {
            return _context.SavedPersons
                .FirstOrDefault(p => p.PersonId == id);
        }

        public void Delete(SavedPerson person)
        {
            _context.SavedPersons.Remove(person);
            _context.SaveChanges();
        }
    }
}