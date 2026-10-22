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

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateStatus(
    int id,
    UpdateStatusDto dto)
    {
        var credential = await _userRepository.GetById(id);

        if (credential == null)
            return NotFound();

        credential.Status = dto.IsActive ? "active" : "deleted";

        await _userRepository.Update(credential);
        await _userRepository.SaveChanges();

        return Ok(MapToProfile(credential));
    }

    [HttpPut("{id:int}/data")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateData(
        int id,
        UpdateProfileDto dto)
    {
        var credential = await _userRepository.GetById(id);

        if (credential == null)
            return NotFound();

        if (credential.UserDetail == null)
            return NotFound("Дані користувача не знайдено");

        if (dto.DateOfBirth.HasValue &&
            dto.DateOfBirth.Value > DateTime.Today.AddYears(-18))
            return BadRequest("Користувач має бути старше 18 років");

        if (!string.IsNullOrWhiteSpace(dto.PassportData) &&
            !System.Text.RegularExpressions.Regex.IsMatch(dto.PassportData, @"^\d+$"))
            return BadRequest("Паспортні дані мають містити тільки цифри");

        credential.UserDetail.Name = dto.Name;

        if (dto.Phone != null)
            credential.UserDetail.Phone = "+38" + dto.Phone;

        credential.UserDetail.PassportData = dto.PassportData;

        if (dto.DateOfBirth.HasValue)
            credential.UserDetail.DateOfBirth = dto.DateOfBirth;

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