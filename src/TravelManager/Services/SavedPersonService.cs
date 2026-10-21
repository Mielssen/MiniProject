using TravelManager.DTOs;
using TravelManager.Exceptions;
using TravelManager.Models;
using TravelManager.Repositories;

namespace TravelManager.Services
{
    public class SavedPersonService
    {
        private readonly SavedPersonRepository _repo;

        public SavedPersonService(SavedPersonRepository repo)
        {
            _repo = repo;
        }

        public List<SavedPersonDto> GetMy(int userId)
        {
            return _repo.GetByUserId(userId)
                .Select(p => new SavedPersonDto
                {
                    PersonId = p.PersonId,
                    Name = p.Name,
                    PassportData = p.PassportData,
                    DateOfBirth = p.DateOfBirth,
                    IsChild = p.IsChild
                })
                .ToList();
        }

        public void Add(int userId, SavedPersonDto dto)
        {
            _repo.Add(new SavedPerson
            {
                UserId = userId,
                Name = dto.Name,
                PassportData = dto.PassportData,
                DateOfBirth = dto.DateOfBirth,
                IsChild = dto.IsChild
            });
        }

        public void Delete(int userId, int personId)
        {
            var person = _repo.GetById(personId);

            if (person == null)
                throw new AppException("NOT_FOUND", "Пресет не знайдено");

            if (person.UserId != userId)
                throw new AppException("FORBIDDEN", "Це не ваш пресет");

            _repo.Delete(person);
        }
    }
}