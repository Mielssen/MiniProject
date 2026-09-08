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
        public IActionResult Create([FromBody] CreateBookingDto dto)
        {
            var credentialId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            _service.Create(credentialId, dto);
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
        public IActionResult GetAll([FromQuery] string? status)
        {
            return Ok(_service.GetAll(status));
        }

        [HttpGet("my")]
        public IActionResult GetMy()
        {
            var credentialId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            return Ok(_service.GetMy(credentialId));
        }

        [HttpDelete("{id}")]
        public IActionResult Cancel(int id)
        {
            var credentialId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            _service.Cancel(credentialId, id);
            return Ok("Бронювання скасовано");
        }
    }
}