using CatalogService.Data;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly CatalogDbContext _context;

    public CatalogRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public IQueryable<Tour> GetTours()
    {
        return _context.Tours
            .Where(t => t.IsActive)
            .Include(t => t.Type)
            .Include(t => t.TourAssets)
            .AsQueryable();
    }

    public async Task<Tour?> GetByIdAsync(int id)
    {
        return await _context.Tours
            .Where(t => t.IsActive)
            .Include(t => t.Type)
            .Include(t => t.TourAssets)
            .FirstOrDefaultAsync(t => t.TourId == id);
    }

    public async Task<int> GetCountAsync(IQueryable<Tour> query)
    {
        return await query.CountAsync();
    }

    public async Task<List<Tour>> GetPageAsync(
        IQueryable<Tour> query,
        int page,
        int pageSize)
    {
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _context.TourTypes
            .AnyAsync(t => t.TypeId == categoryId);
    }

    public async Task AddAsync(Tour tour)
    {
        await _context.Tours.AddAsync(tour);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}