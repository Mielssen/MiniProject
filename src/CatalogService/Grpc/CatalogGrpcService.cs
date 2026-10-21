using CatalogService.Data;
using CatalogService.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Grpc;

public class CatalogGrpcService : CatalogGrpc.CatalogGrpcBase
{
    private readonly CatalogDbContext _context;

    public CatalogGrpcService(CatalogDbContext context)
    {
        _context = context;
    }

    public override async Task<TourResponse> GetTour(
        GetTourRequest request,
        ServerCallContext context)
    {
        var tour = await _context.Tours
            .Where(t => t.IsActive)
            .Include(t => t.Type)
            .FirstOrDefaultAsync(t => t.TourId == request.Id);

        if (tour == null)
        {
            throw new RpcException(
                new Status(
                    StatusCode.NotFound,
                    "Тур не знайдено"));
        }

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