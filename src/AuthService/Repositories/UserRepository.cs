using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Credential?> GetById(int id)
    {
        return await _context.Credentials
            .Include(x => x.UserDetail)
            .FirstOrDefaultAsync(x => x.CredentialId == id);
    }

    public async Task<Credential?> GetByEmail(string email)
    {
        return await _context.Credentials
            .Include(x => x.UserDetail)
            .FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<List<Credential>> GetAll()
    {
        return await _context.Credentials
            .Include(x => x.UserDetail)
            .ToListAsync();
    }

    public async Task<bool> EmailExists(string email)
    {
        return await _context.Credentials
            .AnyAsync(x => x.Email == email);
    }

    public async Task Add(Credential credential)
    {
        await _context.Credentials.AddAsync(credential);
    }

    public Task Update(Credential credential)
    {
        _context.Credentials.Update(credential);
        return Task.CompletedTask;
    }

    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }
}