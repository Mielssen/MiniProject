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
        Console.WriteLine($"gRPC GetTour почався. TourId={request.Id}");

        var tour = await _context.Tours
            .Where(t => t.IsActive)
            .Include(t => t.Type)
            .FirstOrDefaultAsync(t => t.TourId == request.Id);

        Console.WriteLine("gRPC запит до бази завершився.");

        if (tour == null)
        {
            throw new RpcException(
                new Status(
                    StatusCode.NotFound,
                    "Тур не знайдено"));
        }

        Console.WriteLine($"gRPC тур знайдено: {tour.Name}");

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