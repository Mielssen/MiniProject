using System.Security.Claims;
using System.Text;
using BookingService.Data;
using BookingService.Middleware;
using BookingService.Repositories;
using BookingService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch(
    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
    true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseMySql(
        builder.Configuration["BookingService:ConnectionString"] ?? builder.Configuration.GetConnectionString("BookingConnection"),
        new MySqlServerVersion(new Version(8, 0, 0))));

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"]
    ?? throw new InvalidOperationException("У конфігурації відсутній Jwt:Key");

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

builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    var authServiceUrl = builder.Configuration["Services:AuthService"]
        ?? throw new InvalidOperationException("Не налаштовано Services:AuthService");

    client.BaseAddress = new Uri(authServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddSingleton<CatalogGrpcClient>();
builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<BookingOperationsService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();


app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    await next();

    stopwatch.Stop();

    var correlationId = context.Request.Headers["X-Correlation-Id"]
        .FirstOrDefault() ?? "not-provided";

    app.Logger.LogInformation(
        "Request completed. Method={Method}, StatusCode={StatusCode}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
        context.Request.Method,
        context.Response.StatusCode,
        stopwatch.ElapsedMilliseconds,
        correlationId);
});


app.MapControllers();

app.MapGet("/health", async (
    BookingDbContext db,
    CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync(
            cancellationToken);

        if (!canConnect)
        {
            return Results.Json(
                new
                {
                    status = "Unhealthy",
                    service = "BookingService"
                },
                statusCode: 503);
        }

        return Results.Ok(new
        {
            status = "Healthy",
            service = "BookingService"
        });
    }
    catch (OperationCanceledException) when (
        cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch
    {
        return Results.Json(
            new
            {
                status = "Unhealthy",
                service = "BookingService"
            },
            statusCode: 503);
    }
});

app.Run();

public partial class Program { }
