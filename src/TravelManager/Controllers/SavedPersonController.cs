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
            var credentialId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            return Ok(_service.GetMy(credentialId));
        }

        [HttpPost]
        public IActionResult Add([FromBody] SavedPersonDto dto)
        {
            var credentialId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            _service.Add(credentialId, dto);
            return Ok("Пресет збережено");
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var credentialId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            _service.Delete(credentialId, id);
            return Ok("Пресет видалено");
        }
    }
}
