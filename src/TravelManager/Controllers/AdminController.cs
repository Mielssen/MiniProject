using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        public AdminController(AdminService service, ILogger<AdminController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int GetAdminId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("users")]
        public IActionResult GetAll([FromQuery] string? role, [FromQuery] string? status)
        {
            return Ok(_service.GetAllUsers(role, status));
        }

        [HttpGet("users/{id}")]
        public IActionResult GetById(int id)
        {
            return Ok(_service.GetUserById(id));
        }

        [HttpPut("users/{id}/role")]
        public IActionResult UpdateRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            _service.UpdateRole(GetAdminId(), id, dto.Role);
            return Ok("Роль оновлено");
        }

        [HttpPut("users/{id}/data")]
        public IActionResult UpdateData(int id, [FromBody] UpdateProfileDto dto)
        {
            _service.UpdateUserData(GetAdminId(), id, dto);
            return Ok("Дані оновлено");
        }

        [HttpPut("users/{id}/status")]
        public IActionResult SetStatus(int id, [FromBody] UpdateUserStatusDto dto)
        {
            _service.SetUserStatus(GetAdminId(), id, dto.Status);
            return Ok("Статус оновлено");
        }

        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            return Ok(_service.GetStatistics());
        }
    }
}
