using Grpc.Net.Client;
using TravelManager.Grpc;

namespace TravelManager.Services;

public class CatalogGrpcClient
{
    private readonly CatalogGrpc.CatalogGrpcClient _client;

    public CatalogGrpcClient()
    {
        var channel = GrpcChannel.ForAddress("http://localhost:7002");
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