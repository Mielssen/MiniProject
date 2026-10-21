using CatalogService.Data;
using CatalogService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/v1/catalog/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CatalogDbContext _context;

    public CategoriesController(CatalogDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.TourTypes
            .Select(t => new TourTypeDto
            {
                Id = t.TypeId,
                Name = t.Name
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _context.TourTypes
            .Where(t => t.TypeId == id)
            .Select(t => new TourTypeDto
            {
                Id = t.TypeId,
                Name = t.Name
            })
            .FirstOrDefaultAsync();

        if (category == null)
            return NotFound();

        return Ok(category);
    }
}