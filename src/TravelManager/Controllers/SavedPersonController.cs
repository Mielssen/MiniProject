using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravelManager.DTOs;
using TravelManager.Services;

namespace TravelManager.Controllers
{
    [Route("api/savedpersons")]
    [ApiController]
    [Authorize]
    public class SavedPersonController : ControllerBase
    {
        private readonly SavedPersonService _service;

        public SavedPersonController(SavedPersonService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetMy()
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            return Ok(_service.GetMy(userId));
        }

        [HttpPost]
        public IActionResult Add([FromBody] SavedPersonDto dto)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            _service.Add(userId, dto);

            return Ok("Пресет збережено");
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            _service.Delete(userId, id);

            return Ok("Пресет видалено");
        }
    }
}