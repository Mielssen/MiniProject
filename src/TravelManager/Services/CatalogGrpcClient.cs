using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using TravelManager.Grpc;

namespace TravelManager.Services;

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

    public async Task<TourResponse> GetTourAsync(int tourId)
    {
        return await _client.GetTourAsync(
            new GetTourRequest
            {
                Id = tourId
            });
    }
}