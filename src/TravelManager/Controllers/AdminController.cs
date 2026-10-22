
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelManager.DTOs;
using TravelManager.Services;

namespace TravelManager.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _service;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AdminService service,
            ILogger<AdminController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int GetAdminId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private string GetToken() =>
            Request.Headers.Authorization.ToString()
                .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

        [HttpGet("users")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? role,
            [FromQuery] string? status)
        {
            return Ok(await _service.GetAllUsersAsync(role, status, GetToken()));
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _service.GetUserByIdAsync(id, GetToken()));
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateRole(
            int id,
            [FromBody] UpdateUserRoleDto dto)
        {
            await _service.UpdateRoleAsync(
                GetAdminId(), id, dto.Role, GetToken());

            return Ok("Роль оновлено");
        }

        [HttpPut("users/{id}/data")]
        public async Task<IActionResult> UpdateData(
            int id,
            [FromBody] UpdateProfileDto dto)
        {
            await _service.UpdateUserDataAsync(
                GetAdminId(), id, dto, GetToken());

            return Ok("Дані оновлено");
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> SetStatus(
            int id,
            [FromBody] UpdateUserStatusDto dto)
        {
            await _service.SetUserStatusAsync(
                GetAdminId(), id, dto.Status, GetToken());

            return Ok("Статус оновлено");
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            return Ok(await _service.GetStatisticsAsync(GetToken()));
        }
    }
}
