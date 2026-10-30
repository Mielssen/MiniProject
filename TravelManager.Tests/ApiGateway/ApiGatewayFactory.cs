using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace TravelManager.Tests.ApiGateway;

public class ApiGatewayFactory : WebApplicationFactory<global::ApiGateway.Testing.GatewayEntryPoint>
{
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
        RequestHandler
    { get; set; } = DefaultResponse;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "Jwt:Key",
            "TestKeyForApiGatewayIntegrationTests123456789");

        builder.UseSetting("Jwt:Issuer", "TravelManager");
        builder.UseSetting("Jwt:Audience", "TravelManagerUsers");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHttpClientFactory>();

            services.AddSingleton<IHttpClientFactory>(_ =>
                new StubHttpClientFactory(RequestHandler));
        });
    }

    private static Task<HttpResponseMessage> DefaultResponse(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";

        var content = path.Contains("catalog", StringComparison.OrdinalIgnoreCase)
            ? """
              {
                  "items": [
                      {
                          "id": 1,
                          "name": "Test Tour"
                      }
                  ],
                  "count": 1
              }
              """
            : """
              {
                  "bookings": [
                      {
                          "id": 1,
                          "tourId": 1,
                          "status": "confirmed"
                      }
                  ],
                  "count": 1
              }
              """;

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                content,
                System.Text.Encoding.UTF8,
                "application/json")
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly Func<
        HttpRequestMessage,
        CancellationToken,
        Task<HttpResponseMessage>> _handler;

    public StubHttpClientFactory(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
            handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient(
            new StubHttpMessageHandler(_handler));

        if (name == "Dashboard")
            client.Timeout = TimeSpan.FromSeconds(3);

        if (name == "Health")
            client.Timeout = TimeSpan.FromSeconds(2);

        return client;
    }
}

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<
        HttpRequestMessage,
        CancellationToken,
        Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
            handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}