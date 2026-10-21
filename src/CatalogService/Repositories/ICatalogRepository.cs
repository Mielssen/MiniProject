using CatalogService.Models;

namespace CatalogService.Repositories;

public interface ICatalogRepository
{
    IQueryable<Tour> GetTours();
    Task<Tour?> GetByIdAsync(int id);
    Task<int> GetCountAsync(IQueryable<Tour> query);
    Task<List<Tour>> GetPageAsync(IQueryable<Tour> query, int page, int pageSize);
    Task<bool> CategoryExistsAsync(int categoryId);
    Task AddAsync(Tour tour);
    Task SaveChangesAsync();
}