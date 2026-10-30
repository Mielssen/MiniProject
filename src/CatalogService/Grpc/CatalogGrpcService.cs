using CatalogService.Data;
using CatalogService.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Grpc;

public class CatalogGrpcService : CatalogGrpc.CatalogGrpcBase
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<CatalogGrpcService> _logger;

    public CatalogGrpcService(CatalogDbContext context, ILogger<CatalogGrpcService> logger)
    {
        _context = context;
        _logger = logger;
    }


    public override async Task<TourResponse> GetTour(
        GetTourRequest request,
        ServerCallContext context)
    {
        _logger.LogDebug(
            "gRPC GetTour запит. TourId={TourId}",
            request.Id);

        var tour = await _context.Tours
            .Where(t => t.IsActive)
            .Include(t => t.Type)
            .FirstOrDefaultAsync(t => t.TourId == request.Id);

        Console.WriteLine("gRPC запит до бази завершився.");

        if (tour == null)
        {
            _logger.LogWarning(
                "gRPC тур не знайдено. TourId={TourId}",
                request.Id);

            throw new RpcException(
                new Status(StatusCode.NotFound, "Тур не знайдено"));
        }

        _logger.LogDebug(
            "gRPC тур знайдено. TourId={TourId}",
            request.Id);

        return new TourResponse
        {
            Id = tour.TourId,
            Name = tour.Name,
            Description = tour.Description ?? "",
            Price = (double)tour.Price,
            DurationDays = tour.DurationDays,
            AvailableFrom = tour.AvailableFrom.ToString("O"),
            AvailableTo = tour.AvailableTo.ToString("O"),
            TypeId = tour.TypeId,
            TypeName = tour.Type.Name
        };
    }

}