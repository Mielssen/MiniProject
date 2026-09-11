using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

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
    if (context.Request.Path == "/health" && context.Request.Method == "GET")
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
    var sw = Stopwatch.StartNew();

    if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
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
        sw.Stop();

        Console.WriteLine(
            $"[Gateway Log] Method: {method} | " +
            $"Path: {path} | " +
            $"Destination: {destination} | " +
            $"Status: {context.Response.StatusCode} | " +
            $"Duration: {sw.ElapsedMilliseconds}ms | " +
            $"CorrelationId: {correlationId}");
    }
});

await app.UseOcelot();

app.Run();