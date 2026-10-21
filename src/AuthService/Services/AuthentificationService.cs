using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using Microsoft.EntityFrameworkCore;
using AuthService.Exceptions;
namespace AuthService.Services;

public class AuthentificationService : IAuthentificationService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly AuthDbContext _context;

    public AuthentificationService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        AuthDbContext context)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<TokenResponceDto> Register(RegisterRequestDto dto)
    {
        if (await _userRepository.EmailExists(dto.Email))
            throw new AppException("EMAIL_EXISTS", "Email already exists.");

        var credential = new Credential
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "client",
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        credential.UserDetail = new UserDetail
        {
            Name = dto.Name,
            Phone = dto.Phone,
            PassportData = dto.PassportData,
            DateOfBirth = dto.DateOfBirth
        };

        await _userRepository.Add(credential);
        await _userRepository.SaveChanges();

        var accessToken = _tokenService.GenerateAccessToken(credential);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            CredentialId = credential.CredentialId,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _tokenService.GetRefreshTokenExpiration()
        };

        await _refreshTokenRepository.Add(refreshTokenEntity);
        await _refreshTokenRepository.SaveChanges();

        return new TokenResponceDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration()
        };
    }

    public async Task<TokenResponceDto> Login(LoginDto dto)
    {
        var credential = await _userRepository.GetByEmail(dto.Email);

        if (credential == null ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, credential.PasswordHash))
        {
            throw new AppException("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        if (credential.Status == "deleted")
        {
            throw new AppException("USER_DELETED", "User account is deleted.");
        }

        var accessToken = _tokenService.GenerateAccessToken(credential);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            CredentialId = credential.CredentialId,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _tokenService.GetRefreshTokenExpiration()
        };

        await _refreshTokenRepository.Add(refreshTokenEntity);
        await _refreshTokenRepository.SaveChanges();

        return new TokenResponceDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration()
        };
    }

    public async Task<TokenResponceDto> Refresh(string token)
    {
        var oldToken = await _refreshTokenRepository.GetByToken(token);

        if (oldToken == null ||
            oldToken.RevokedAt != null ||
            oldToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AppException("INVALID_REFRESH_TOKEN", "Invalid refresh token.");
        }

        var credential = await _userRepository.GetById(oldToken.CredentialId);

        if (credential == null || credential.Status == "deleted")
        {
            throw new AppException("USER_NOT_FOUND", "User not found.");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(credential);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.ReplacedByToken = newRefreshToken;

        var newTokenEntity = new RefreshToken
        {
            CredentialId = credential.CredentialId,
            Token = newRefreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _tokenService.GetRefreshTokenExpiration()
        };

        await _refreshTokenRepository.Add(newTokenEntity);
        await _refreshTokenRepository.SaveChanges();

        return new TokenResponceDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration()
        };
    }

    public async Task Logout(string token)
    {
        var refreshToken = await _refreshTokenRepository.GetByToken(token);

        if (refreshToken != null && refreshToken.RevokedAt == null)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.SaveChanges();
        }
    }
}