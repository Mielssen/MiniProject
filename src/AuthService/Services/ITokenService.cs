using AuthService.Models;

namespace AuthService.Services;

public interface ITokenService
{
    string GenerateAccessToken(Credential credential);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiration();
    DateTime GetRefreshTokenExpiration();
}