using System.Diagnostics;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ApiGateway.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT key is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Не авторизовано",
                    detail: "Для доступу до ресурсу необхідна авторизація."
                ).ExecuteAsync(context.HttpContext);
            },

            OnForbidden = async context =>
            {
                await Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Доступ заборонено",
                    detail: "У вас недостатньо прав для виконання цієї операції."
                ).ExecuteAsync(context.HttpContext);
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";

        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Забагато запитів",
            detail: "Перевищено допустиму кількість запитів. Спробуйте пізніше."
        ).ExecuteAsync(context.HttpContext);
    };

    options.GlobalLimiter =
        System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
            {
                var path = httpContext.Request.Path.Value ?? "";

                var isLogin = path.EndsWith(
                    "/login",
                    StringComparison.OrdinalIgnoreCase);

                var userId = httpContext.User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                var ip = httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                var key = isLogin
                    ? $"login:{ip}"
                    : !string.IsNullOrWhiteSpace(userId)
                        ? $"user:{userId}"
                        : $"ip:{ip}";

                var permitLimit = isLogin ? 5 : 30;

                return System.Threading.RateLimiting.RateLimitPartition
                    .GetFixedWindowLimiter(
                        key,
                        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        });
            });
});
builder.Configuration.AddJsonFile(
    "ocelot.json",
    optional: false,
    reloadOnChange: true);

builder.Services.AddControllers();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CatalogCacheKeyRegistry>();
builder.Services.AddHttpClient("Dashboard", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddHttpClient("Health", client =>
{
    client.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance =
            context.HttpContext.Request.Path;
    };
});

builder.Services.AddOcelot(builder.Configuration).AddPolly();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseExceptionHandler();

app.UseStatusCodePages(async statusCodeContext =>
{
    var context = statusCodeContext.HttpContext;
    var statusCode = context.Response.StatusCode;

    var title = statusCode switch
    {
        401 => "Не авторизовано",
        403 => "Доступ заборонено",
        404 => "Ресурс не знайдено",
        405 => "Метод не дозволений",
        429 => "Забагато запитів",
        502 => "Помилка шлюзу",
        503 => "Сервіс недоступний",
        504 => "Час очікування вичерпано",
        _ => "Помилка запиту"
    };

    await Results.Problem(
        statusCode: statusCode,
        title: title,
        detail: statusCode switch
        {
            401 => "Для доступу до ресурсу необхідна авторизація.",
            403 => "У вас недостатньо прав для виконання цієї операції.",
            404 => "Запитуваний ресурс не знайдено.",
            429 => "Перевищено допустиму кількість запитів. Спробуйте пізніше.",
            502 or 503 or 504 => "Один із сервісів Gateway тимчасово недоступний.",
            _ => "Не вдалося виконати запит."
        }
    ).ExecuteAsync(context);
});


app.MapGet("/health", async (
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient("Health");

    async Task<bool> CheckService(string url)
    {
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    var catalogTask = CheckService("http://127.0.0.1:5002/health");
    var bookingTask = CheckService("http://127.0.0.1:5100/health");
    var authTask = CheckService("https://localhost:7001/health");

    await Task.WhenAll(catalogTask, bookingTask, authTask);

    var catalogHealthy = await catalogTask;
    var bookingHealthy = await bookingTask;
    var authHealthy = await authTask;

    var healthy = catalogHealthy && bookingHealthy && authHealthy;

    return Results.Json(new
    {
        status = healthy ? "Healthy" : "Unhealthy",
        gateway = "Running",
        dependencies = new
        {
            catalog = catalogHealthy ? "Healthy" : "Unhealthy",
            booking = bookingHealthy ? "Healthy" : "Unhealthy",
            auth = authHealthy ? "Healthy" : "Unhealthy"
        }
    }, statusCode: healthy ? 200 : 503);
});

app.UseCors("AllowFrontend");

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    if (!context.Request.Headers.TryGetValue(
        "X-Correlation-Id",
        out var correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = correlationId;
    }

    context.Response.Headers["X-Correlation-Id"] = correlationId;

    var method = context.Request.Method;
    var path = context.Request.Path;
    var destination = $"http://localhost:5272{path}";

    try
    {
        await next();
    }
    finally
    {
        stopwatch.Stop();

        Console.WriteLine(
            $"[Gateway Log] Method: {method} | " +
            $"Path: {path} | " +
            $"Destination: {destination} | " +
            $"Status: {context.Response.StatusCode} | " +
            $"Duration: {stopwatch.ElapsedMilliseconds}ms | " +
            $"CorrelationId: {correlationId}");
    }
});
app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.UseMiddleware<CatalogCacheMiddleware>();
await app.UseOcelot();

app.Run();

public partial class Program;
