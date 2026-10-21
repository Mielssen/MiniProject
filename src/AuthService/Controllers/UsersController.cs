using System.Security.Claims;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var credentialId = GetCredentialId();

        var credential = await _userRepository.GetById(credentialId);

        if (credential == null)
            return NotFound();

        return Ok(MapToProfile(credential));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var credential = await _userRepository.GetById(id);

        if (credential == null)
            return NotFound();

        return Ok(MapToProfile(credential));
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userRepository.GetAll();

        return Ok(users.Select(MapToProfile));
    }

    [HttpPut("{id:int}/roles")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateRoles(
        int id,
        UpdateRolesDto dto)
    {
        var credential = await _userRepository.GetById(id);

        if (credential == null)
            return NotFound();

        if (dto.Roles == null || dto.Roles.Count != 1)
            return BadRequest("User must have exactly one role.");

        var role = dto.Roles[0].ToLower();

        if (role != "client" &&
            role != "manager" &&
            role != "admin")
        {
            return BadRequest("Invalid role.");
        }

        credential.Role = role;

        await _userRepository.Update(credential);
        await _userRepository.SaveChanges();

        return Ok(MapToProfile(credential));
    }

    private int GetCredentialId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var id))
            throw new UnauthorizedAccessException();

        return id;
    }

    private static ProfileDto MapToProfile(Credential credential)
    {
        return new ProfileDto
        {
            Id = credential.CredentialId,
            Email = credential.Email,
            DisplayName = credential.UserDetail?.Name ?? "",
            Phone = credential.UserDetail?.Phone,
            PassportData = credential.UserDetail?.PassportData,
            DateOfBirth = credential.UserDetail?.DateOfBirth,
            CreatedAt = credential.CreatedAt ?? DateTime.UtcNow,
            IsActive = credential.Status == "active",
            Roles = new List<string> { credential.Role }
        };
    }
}