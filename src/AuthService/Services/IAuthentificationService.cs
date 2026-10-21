using AuthService.DTOs;

namespace AuthService.Services;

public interface IAuthentificationService
{
    Task<TokenResponceDto> Register(RegisterRequestDto dto);
    Task<TokenResponceDto> Login(LoginDto dto);
    Task<TokenResponceDto> Refresh(string token);
    Task Logout(string token);
}