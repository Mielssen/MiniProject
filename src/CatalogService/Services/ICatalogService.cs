using CatalogService.DTOs;

namespace CatalogService.Services;

public interface ICatalogService
{
    Task<object> GetAllAsync(int page, int pageSize, string? search, int? categoryId, string? sortBy, bool desc);

    Task<CatalogItemDto?> GetByIdAsync(int id);

    Task<CatalogItemDto> CreateAsync(CreateCatalogItemDto dto);

    Task<bool> UpdateAsync(int id, UpdateCatalogItemDto dto);

    Task<bool> DeleteAsync(int id);
}