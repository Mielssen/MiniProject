using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelManager.DTOs;
using TravelManager.Models;
using TravelManager.Repositories;
using TravelManager.Services;

namespace TravelManager.Controllers
{
    [Route("api/tours")]
    [ApiController]
    public class TourController : ControllerBase
    {
        private readonly TourService _service;
        private readonly TravelmanagerContext _context;

        public TourController(TourService service, TravelmanagerContext context)
        {
            _service = service;
            _context = context;
        }

        [Authorize(Roles = "manager")]
        [HttpPost]
        public IActionResult Create([FromBody] CreateTourDto dto)
        {
            var id = _service.CreateTour(dto);
            return Ok(new { tourId = id });
        }

        [HttpGet("types")]
        public IActionResult GetTypes()
        {
            var types = _context.TourTypes
                .Select(t => new { t.TypeId, t.Name })
                .ToList();

            return Ok(types);
        }

        [HttpGet]
        public IActionResult GetAll([FromQuery] string? search)
        {
            if (!string.IsNullOrWhiteSpace(search))
                return Ok(_service.Search(search));

            return Ok(_service.GetAll());
        }

        [Authorize(Roles = "manager")]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            return Ok(_service.GetById(id));
        }

        [Authorize(Roles = "manager")]
        [HttpPut("{id}")]
        public IActionResult Update(int id, CreateTourDto dto)
        {
            _service.UpdateTour(id, dto);
            return Ok("Tour updated");
        }

        [Authorize(Roles = "manager")]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _service.DeleteTour(id);
            return Ok("Deleted");
        }

        [Authorize(Roles = "manager")]
        [HttpPost("{tourId}/images")]
        [RequestSizeLimit(500_000_000)]
        public async Task<IActionResult> UploadTourImage(int tourId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            var allowedVideoTypes = new[] { "video/mp4", "video/webm", "video/quicktime" };
            var allowedTypes = allowedImageTypes.Concat(allowedVideoTypes);

            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Непідтримуваний формат файлу");

            var folder = file.ContentType.StartsWith("video") ? "videos" : "images";
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var path = Path.Combine(dir, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"https://localhost:7041/{folder}/{fileName}";
            var assetType = file.ContentType.StartsWith("video") ? "video" : "image";

            _service.AddAsset(tourId, url, assetType);

            return Ok(new { url });
        }
    }
}
