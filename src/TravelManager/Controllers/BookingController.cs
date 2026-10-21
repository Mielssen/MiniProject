using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelManager.DTOs;
using TravelManager.Services;

namespace TravelManager.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly BookingService _service;

        public BookingController(BookingService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var token = Request.Headers.Authorization
                .ToString()
                .Replace("Bearer ", "");

            await _service.Create(userId, token, dto);

            return Ok("Бронювання створено");
        }

        [Authorize(Roles = "manager")]
        [HttpPut("{id}/status")]
        public IActionResult UpdateStatus(int id, [FromBody] UpdateBookingStatusDto dto)
        {
            _service.UpdateStatus(id, dto.Status);
            return Ok("Статус оновлено");
        }

        [Authorize(Roles = "manager")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var token = Request.Headers.Authorization
                .ToString()
                .Replace("Bearer ", "");

            return Ok(await _service.GetAll(status, token));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var token = Request.Headers.Authorization
                .ToString()
                .Replace("Bearer ", "");

            return Ok(await _service.GetMy(userId, token));
        }

        [HttpDelete("{id}")]
        public IActionResult Cancel(int id)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            _service.Cancel(userId, id);

            return Ok("Бронювання скасовано");
        }
    }
}