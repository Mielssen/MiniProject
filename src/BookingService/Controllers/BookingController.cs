using System.Security.Claims;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[Route("api/bookings")]
[ApiController]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly BookingOperationsService _service;

    public BookingController(BookingOperationsService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
    [FromBody] CreateBookingDto dto,
    [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var token = GetToken();

        await _service.Create(userId, token, dto, idempotencyKey);

        return Ok("Бронювання створено");
    }

    [Authorize(Roles = "manager")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateBookingStatusDto dto)
    {
        await _service.UpdateStatus(id, dto.Status);

        return Ok("Статус оновлено");
    }

    [Authorize(Roles = "manager,admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status)
    {
        var token = GetToken();

        return Ok(await _service.GetAll(status, token));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var token = GetToken();

        return Ok(await _service.GetMy(userId, token));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _service.Cancel(userId, id);

        return Ok("Бронювання скасовано");
    }

    private string GetToken()
    {
        return Request.Headers.Authorization.ToString();
    }
}