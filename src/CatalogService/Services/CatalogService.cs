using CatalogService.DTOs;
using CatalogService.Models;
using CatalogService.Repositories;

namespace CatalogService.Services;

public class CatalogServiceManager : ICatalogService
{
    private readonly ICatalogRepository _repository;

    public CatalogServiceManager(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<object> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        int? categoryId,
        string? sortBy,
        bool desc)
    {
        if (page < 1)
            throw new ArgumentException("page повинен бути не менше 1");

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("pageSize повинен бути від 1 до 100");

        var query = _repository.GetTours();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                t.Name.Contains(search) ||
                (t.Description != null && t.Description.Contains(search)));
        }

        if (categoryId.HasValue)
            query = query.Where(t => t.TypeId == categoryId.Value);

        query = sortBy?.ToLower() switch
        {
            "name" => desc
                ? query.OrderByDescending(t => t.Name)
                : query.OrderBy(t => t.Name),

            "price" => desc
                ? query.OrderByDescending(t => t.Price)
                : query.OrderBy(t => t.Price),

            "duration" => desc
                ? query.OrderByDescending(t => t.DurationDays)
                : query.OrderBy(t => t.DurationDays),

            _ => query.OrderBy(t => t.TourId)
        };

        var totalCount = await _repository.GetCountAsync(query);
        var tours = await _repository.GetPageAsync(query, page, pageSize);

        var items = tours.Select(MapToDto).ToList();

        return new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            items
        };
    }

    public async Task<CatalogItemDto?> GetByIdAsync(int id)
    {
        var tour = await _repository.GetByIdAsync(id);

        return tour == null ? null : MapToDto(tour);
    }

    public async Task<CatalogItemDto> CreateAsync(CreateCatalogItemDto dto)
    {
        if (dto.AvailableFrom > dto.AvailableTo)
            throw new ArgumentException("Дата початку не може бути пізніше дати завершення");

        if (!await _repository.CategoryExistsAsync(dto.TypeId))
            throw new ArgumentException("Вказано неіснуючу категорію");

        var tour = new Tour
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            DurationDays = dto.DurationDays,
            AvailableFrom = dto.AvailableFrom,
            AvailableTo = dto.AvailableTo,
            TypeId = dto.TypeId
        };

        await _repository.AddAsync(tour);
        await _repository.SaveChangesAsync();

        var createdTour = await _repository.GetByIdAsync(tour.TourId);

        return MapToDto(createdTour!);
    }

    public async Task<bool> UpdateAsync(int id, UpdateCatalogItemDto dto)
    {
        var tour = await _repository.GetByIdAsync(id);

        if (tour == null)
            return false;

        if (dto.AvailableFrom > dto.AvailableTo)
            throw new ArgumentException("Дата початку не може бути пізніше дати завершення");

        if (!await _repository.CategoryExistsAsync(dto.TypeId))
            throw new ArgumentException("Вказано неіснуючу категорію");

        tour.Name = dto.Name;
        tour.Description = dto.Description;
        tour.Price = dto.Price;
        tour.DurationDays = dto.DurationDays;
        tour.AvailableFrom = dto.AvailableFrom;
        tour.AvailableTo = dto.AvailableTo;
        tour.TypeId = dto.TypeId;

        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tour = await _repository.GetByIdAsync(id);

        if (tour == null)
            return false;

        tour.IsActive = false;

        await _repository.SaveChangesAsync();

        return true;
    }

    private static CatalogItemDto MapToDto(Tour tour)
    {
        return new CatalogItemDto
        {
            Id = tour.TourId,
            Name = tour.Name,
            Description = tour.Description,
            Price = tour.Price,
            DurationDays = tour.DurationDays,
            AvailableFrom = tour.AvailableFrom,
            AvailableTo = tour.AvailableTo,
            TypeId = tour.TypeId,
            TypeName = tour.Type.Name,
            Assets = tour.TourAssets.Select(a => new TourAssetDto
            {
                Id = a.AssetId,
                AssetType = a.AssetType,
                Url = a.Url
            }).ToList()
        };
    }
}