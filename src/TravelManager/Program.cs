using TravelManager.Models;
using TravelManager.Repositories;
using TravelManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using TravelManager.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using TravelManager.Middleware;
using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1-legacy", new OpenApiInfo
    {
        Title = "TravelManager — Legacy API",
        Version = "v1-legacy",
        Description = "Монолітний API. Частина функціональності перенесена в AuthService, CatalogService та BookingService."
    });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues["controller"];

        return controller is not ("Auth" or "Booking" or "Tour");
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

builder.Services.AddDbContext<TravelmanagerContext>(options =>
{
    options
        .UseMySql(
            builder.Configuration["TravelManager:ConnectionString"] ?? builder.Configuration.GetConnectionString("DefaultConnection"),
            new MySqlServerVersion(new Version(8, 0, 0))
        )
        .EnableDetailedErrors();
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 500_000_000;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500_000_000;
});

builder.Services.AddScoped<SavedPersonRepository>();
builder.Services.AddScoped<SavedPersonService>();
builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<CredentialsRepository>();
builder.Services.AddScoped<AuthentificationService>();
builder.Services.AddScoped<TourRepository>();
builder.Services.AddScoped<TourService>();
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddScoped<AdminService>();

builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    var authServiceUrl = builder.Configuration["Services:AuthService"]
        ?? throw new InvalidOperationException("Не налаштовано Services:AuthService");

    client.BaseAddress = new Uri(authServiceUrl);
});

builder.Services.AddHttpClient<BookingServiceClient>(client =>
{
    var bookingServiceUrl = builder.Configuration["Services:BookingService"]
        ?? throw new InvalidOperationException("Не налаштовано Services:BookingService");

    client.BaseAddress = new Uri(bookingServiceUrl);
});

builder.Services.AddSingleton<CatalogGrpcClient>();

var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
            Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is AppException appEx)
        {
            context.Response.StatusCode = 400;
            logger.LogWarning("AppException: Code={Code}", appEx.Code);
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Message = appEx.Message,
                Code = appEx.Code
            });
        }
        else
        {
            context.Response.StatusCode = 500;
            logger.LogError(exception, "Необроблена помилка сервера");
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Message = "Internal server error"
            });
        }
    });
});

app.UseStaticFiles();
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1-legacy/swagger.json", "TravelManager Legacy API v1-legacy");
    c.DocumentTitle = "TravelManager — Legacy API";
});

//app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
