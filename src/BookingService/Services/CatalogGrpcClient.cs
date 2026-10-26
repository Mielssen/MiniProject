using BookingService.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

namespace BookingService.Services;

public class CatalogGrpcClient
{
    private readonly CatalogGrpc.CatalogGrpcClient _client;

    public CatalogGrpcClient(IConfiguration configuration)
    {
        var catalogServiceUrl = configuration["Services:CatalogService"]
            ?? throw new InvalidOperationException("Не налаштовано Services:CatalogService");

        var channel = GrpcChannel.ForAddress(catalogServiceUrl);
        _client = new CatalogGrpc.CatalogGrpcClient(channel);
    }

    public async Task<TourResponse> GetTourAsync(
        int tourId,
        CancellationToken cancellationToken = default)
    {
        return await _client.GetTourAsync(
            new GetTourRequest
            {
                Id = tourId
            },
            deadline: DateTime.UtcNow.AddSeconds(2),
            cancellationToken: cancellationToken);
    }
}