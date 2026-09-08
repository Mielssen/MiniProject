using TravelManager.DTOs;
using TravelManager.Models;
using TravelManager.Repositories;
using TravelManager.Exceptions;

namespace TravelManager.Services
{
    public class TourService
    {
        private readonly TourRepository _repo;
        private readonly TravelmanagerContext _context;
        private readonly ILogger<TourService> _logger;
        public TourService(TourRepository repo, TravelmanagerContext context, ILogger<TourService> logger)
        {
            _repo = repo;
            _context = context;
            _logger = logger;
        }

        public int CreateTour(CreateTourDto dto)
        {
            if (dto == null)
                throw new AppException("NULL", "Дані не передані");

            var typeExists = _context.TourTypes.Any(t => t.TypeId == dto.TypeId);
            if (!typeExists)
                throw new AppException("NOT_FOUND", "Тип туру не знайдено");

            var tour = new Tour
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Price = dto.Price,
                DurationDays = dto.DurationDays,
                AvailableFrom = dto.AvailableFrom,
                AvailableTo = dto.AvailableTo,
                TypeId = dto.TypeId
            };

            _repo.Add(tour);
            _logger.LogInformation("Створено тур: Name={Name}, Price={Price}", tour.Name, tour.Price);
            return tour.TourId;
        }

        public void UpdateTour(int id, CreateTourDto dto)
        {
            var tour = _repo.GetById(id);   

            if (tour == null)
                throw new AppException("NOT_FOUND", "Тур не знайдено");

            tour.Name = dto.Name.Trim();
            tour.Description = dto.Description;
            tour.Price = dto.Price;
            tour.DurationDays = dto.DurationDays;
            tour.AvailableFrom = dto.AvailableFrom;
            tour.AvailableTo = dto.AvailableTo;
            tour.TypeId = dto.TypeId;

            _repo.Update(tour);

            var oldAssets = _context.TourAssets
                .Where(x => x.TourId == id)
                .ToList();

            var newUrls = dto.Assets?.Select(a => a.Url).ToHashSet() ?? new HashSet<string>();

            foreach (var asset in oldAssets)
            {
                if (!newUrls.Contains(asset.Url))
                {
                    var folder = asset.AssetType == "video" ? "videos" : "images";
                    var fileName = Path.GetFileName(asset.Url);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, fileName);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
            }

            _context.TourAssets.RemoveRange(oldAssets);

            if (dto.Assets != null)
            {
                foreach (var asset in dto.Assets)
                {
                    _context.TourAssets.Add(new TourAsset
                    {
                        TourId = id,
                        AssetType = asset.AssetType,
                        Url = asset.Url
                    });
                }
            }

            _context.SaveChanges();
            _logger.LogInformation("Оновлено тур ID={TourId}", id);
        }

        public List<TourDto> GetAll()
        {
            var tours = _repo.GetAll();

            return tours.Select(t => new TourDto
            {
                TourId = t.TourId,
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                DurationDays = t.DurationDays,
                AvailableFrom = t.AvailableFrom,
                AvailableTo = t.AvailableTo,
                TypeId = t.TypeId,
                Assets = t.TourAssets != null
                    ? t.TourAssets.Select(a => new TourAssetDto
                    {
                        AssetType = a.AssetType,
                        Url = a.Url
                    }).ToList()
                    : new List<TourAssetDto>()
            }).ToList();
        }

        public TourDto GetById(int id)
        {
            var t = _repo.GetById(id);

            if (t == null)
                throw new AppException("NOT_FOUND", "Тур не знайдено");

            return new TourDto
            {
                TourId = t.TourId,
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                DurationDays = t.DurationDays,
                AvailableFrom = t.AvailableFrom,
                AvailableTo = t.AvailableTo,
                TypeId = t.TypeId,
                Assets = t.TourAssets != null
                    ? t.TourAssets.Select(a => new TourAssetDto
                    {
                        AssetType = a.AssetType,
                        Url = a.Url
                    }).ToList()
                    : new List<TourAssetDto>()
            };
        }
        public List<TourDto> Search(string query)
        {
            var tours = _repo.Search(query);

            return tours.Select(t => new TourDto
            {
                TourId = t.TourId,
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                DurationDays = t.DurationDays,
                AvailableFrom = t.AvailableFrom,
                AvailableTo = t.AvailableTo,
                TypeId = t.TypeId,
                Assets = t.TourAssets != null
                    ? t.TourAssets.Select(a => new TourAssetDto
                    {
                        AssetType = a.AssetType,
                        Url = a.Url
                    }).ToList()
                    : new List<TourAssetDto>()
            }).ToList();
        }
        public void AddAsset(int tourId, string url, string assetType)
        {
            var tour = _repo.GetById(tourId);

            if (tour == null)
                throw new AppException("NOT_FOUND", "Тур не знайдено");

            _context.TourAssets.Add(new TourAsset
            {
                TourId = tourId,
                AssetType = assetType,
                Url = url
            });

            _context.SaveChanges();
        }
        public void DeleteTour(int id)
        {
            var tour = _repo.GetById(id);
            if (tour == null)
                throw new AppException("NOT_FOUND", "Тур не знайдено");

            foreach (var asset in tour.TourAssets)
            {
                var folder = asset.AssetType == "video" ? "videos" : "images";
                var fileName = Path.GetFileName(asset.Url);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, fileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            var bookings = _context.Bookings.Where(b => b.TourId == id).ToList();
            var bookingIds = bookings.Select(b => b.BookingId).ToList();

            _context.BookingPersons.RemoveRange(
                _context.BookingPersons.Where(p => bookingIds.Contains(p.BookingId))
            );

            _context.Bookings.RemoveRange(bookings);

            _context.TourAssets.RemoveRange(
                _context.TourAssets.Where(x => x.TourId == id)
            );

            _repo.Delete(tour);
            _logger.LogInformation("Видалено тур ID={TourId}", id);
        }
    }
}