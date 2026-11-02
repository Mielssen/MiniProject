using CatalogService.DTOs;
using CatalogService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CatalogService.Data;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/v1/catalog/items")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _service;
    private readonly CatalogDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public CatalogController(
    ICatalogService service,
    CatalogDbContext context,
    IWebHostEnvironment environment)
    {
        _service = service;
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        int? categoryId = null,
        string? sortBy = null,
        bool desc = false)
    {
        try
        {
            var result = await _service.GetAllAsync(
                page,
                pageSize,
                search,
                categoryId,
                sortBy,
                desc);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Create(
    [FromBody] CreateCatalogItemDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCatalogItemDto dto)
    {
        try
        {
            var result = await _service.UpdateAsync(id, dto);

            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }
    [HttpPost("{id:int}/images")]
    [Authorize(Roles = "manager")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> UploadAsset(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не вибрано.");

        var allowedTypes = new Dictionary<string, string[]>
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"],
            ["image/gif"] = [".gif"],
            ["video/mp4"] = [".mp4"],
            ["video/webm"] = [".webm"]
        };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedTypes.TryGetValue(file.ContentType, out var extensions) ||
            !extensions.Contains(extension))
        {
            return BadRequest("Дозволені JPG, PNG, WEBP, GIF, MP4 та WEBM.");
        }

        var tourExists = await _context.Tours
            .AnyAsync(t => t.TourId == id && t.IsActive);

        if (!tourExists)
            return NotFound("Тур не знайдено.");

        var assetType = file.ContentType.StartsWith("image/")
            ? "image"
            : "video";

        var folderName = assetType == "image" ? "images" : "videos";
        var fileName = $"{Guid.NewGuid()}{extension}";

        var uploadDirectory = Path.Combine(
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            "uploads",
            folderName);

        Directory.CreateDirectory(uploadDirectory);

        var filePath = Path.Combine(uploadDirectory, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var asset = new TourAsset
        {
            TourId = id,
            AssetType = assetType,
            Url = $"/uploads/{folderName}/{fileName}"
        };

        _context.TourAssets.Add(asset);
        await _context.SaveChangesAsync();

        return Ok(new TourAssetDto
        {
            Id = asset.AssetId,
            AssetType = asset.AssetType,
            Url = asset.Url
        });
    }
}