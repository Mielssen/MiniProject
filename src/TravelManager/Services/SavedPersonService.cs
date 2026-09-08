using Microsoft.EntityFrameworkCore;
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
        public List<SavedPersonDto> GetMy(int credentialId)
        {
            return _repo.GetByCredentialId(credentialId).
                Select(p => new SavedPersonDto
                {
                    PersonId = p.PersonId,
                    Name = p.Name,
                    PassportData = p.PassportData,
                    DateOfBirth = p.DateOfBirth,
                    IsChild = p.IsChild,
                }).ToList();
        }

        public void Add(int credentialId, SavedPersonDto dto)
        {
            _repo.Add(new SavedPerson
            {
                CredentialId = credentialId,
                Name = dto.Name,
                PassportData = dto.PassportData,
                DateOfBirth = dto.DateOfBirth,
                IsChild = dto.IsChild,
            });
        }

        public void Delete(int credentialId, int personId)
        {
            var person = _repo.GetById(personId);

            if (person == null)
                throw new AppException("NOT_FOUND", "Пресет не знайдено");

            if (person.CredentialId != credentialId)
                throw new AppException("FORBIDDEN", "Це не ваш пресет");

            _repo.Delete(person);
        }
    }
}
