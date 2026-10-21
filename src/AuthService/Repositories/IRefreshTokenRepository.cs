using AuthService.Models;

namespace AuthService.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByToken(string token);
    Task<List<RefreshToken>> GetActiveByUserId(int userId);
    Task Add(RefreshToken token);
    Task SaveChanges();
}