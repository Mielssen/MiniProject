using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGateway.Swagger;

public class DashboardExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.Trim('/') != "api/dashboard")
            return;

        if (!operation.Responses.TryGetValue("200", out var response))
            return;

        if (!response.Content.TryGetValue("application/json", out var mediaType))
            return;

        mediaType.Examples = new Dictionary<string, OpenApiExample>
        {
            ["Full response"] = new OpenApiExample
            {
                Summary = "Повна відповідь — обидва сервіси доступні",
                Value = OpenApiAnyFactory.CreateFromJson("""
                {
                    "catalog": [],
                    "bookings": [],
                    "partial": false,
                    "errors": {
                        "catalog": null,
                        "bookings": null
                    }
                }
                """)
            },
            ["Partial response"] = new OpenApiExample
            {
                Summary = "Часткова відповідь — CatalogService недоступний",
                Value = OpenApiAnyFactory.CreateFromJson("""
                {
                    "catalog": null,
                    "bookings": [],
                    "partial": true,
                    "errors": {
                        "catalog": "Сервіс недоступний",
                        "bookings": null
                    }
                }
                """)
            }
        };
    }
}