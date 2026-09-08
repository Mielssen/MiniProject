using Microsoft.AspNetCore.Mvc;
using TravelManager.Repositories;
using TravelManager.DTOs;
using TravelManager.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TravelManager.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CredentialsRepository _repo;
        private readonly AuthentificationService _authService;

        public AuthController(CredentialsRepository repo, AuthentificationService authService)
        {
            _repo = repo;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto dto)
        {
            var result = await _authService.RegisterUser(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            var token = _authService.Login(dto);
            return Ok(token);
        }

        [Authorize]
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var credentialId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var credential = _repo.GetById(credentialId);
            var detail = _repo.GetUserDetail(credentialId);

            if (credential == null || detail == null)
                return NotFound();

            return Ok(new ProfileDto
            {
                Email = credential.Email,
                Name = detail.Name,
                Phone = detail.Phone,
                PassportData = detail.PassportData,
                DateOfBirth = detail.DateOfBirth
            });
        }

        [Authorize]
        [HttpPut("profile")]
        public IActionResult UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var credentialId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var detail = _repo.GetUserDetail(credentialId);

            if (detail == null)
                return NotFound();

            detail.Name = dto.Name;
            detail.Phone = dto.Phone != null ? "+38" + dto.Phone : detail.Phone;
            detail.PassportData = dto.PassportData;

            _repo.UpdateUserDetail(detail);

            return Ok("Профіль оновлено");
        }
    }
}