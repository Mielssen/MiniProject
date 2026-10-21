using AuthService.Models;

namespace AuthService.Repositories;

public interface IUserRepository
{
    Task<Credential?> GetById(int id);
    Task<Credential?> GetByEmail(string email);
    Task<List<Credential>> GetAll();
    Task<bool> EmailExists(string email);
    Task Add(Credential credential);
    Task Update(Credential credential);
    Task SaveChanges();
}