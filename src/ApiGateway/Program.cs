using System.Diagnostics;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ApiGateway.Middleware;

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
builder.Services.AddHttpClient();

builder.Services.AddProblemDetails();

builder.Services.AddOcelot(builder.Configuration);

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

app.UseCors("AllowFrontend");

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health" &&
        context.Request.Method == "GET")
    {
        context.Response.StatusCode = 200;

        await context.Response.WriteAsJsonAsync(new
        {
            status = "Healthy",
            gateway = "Running"
        });

        return;
    }

    await next();
});

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
