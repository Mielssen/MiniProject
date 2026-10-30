
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace ApiGateway.Middleware;

public class CatalogCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly CatalogCacheKeyRegistry _keyRegistry;

    public CatalogCacheMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        CatalogCacheKeyRegistry keyRegistry)
    {
        _next = next;
        _cache = cache;
        _keyRegistry = keyRegistry;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        if (!path.StartsWithSegments("/api/v1/catalog"))
        {
            await _next(context);
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method) &&
            path.Equals("/api/v1/catalog/items"))
        {
            var cacheKey = $"catalog:{path}{context.Request.QueryString}";

            if (_cache.TryGetValue(cacheKey, out CachedResponse? cached) && cached != null)
            {
                context.Response.Headers["X-Cache"] = "HIT";
                context.Response.StatusCode = cached.StatusCode;
                context.Response.ContentType = cached.ContentType;

                await context.Response.Body.WriteAsync(
                    cached.Body,
                    context.RequestAborted);

                return;
            }
            context.Response.Headers["X-Cache"] = "MISS";
            var originalBody = context.Response.Body;

            await using var buffer = new MemoryStream();

            context.Response.Body = buffer;

            try
            {
                await _next(context);

                buffer.Position = 0;

                if (context.Response.StatusCode == StatusCodes.Status200OK)
                {
                    var body = buffer.ToArray();

                    _cache.Set(
                        cacheKey,
                        new CachedResponse(
                            context.Response.StatusCode,
                            context.Response.ContentType,
                            body),
                        TimeSpan.FromSeconds(60));

                    _keyRegistry.Register(cacheKey);
                }

                buffer.Position = 0;

                await buffer.CopyToAsync(
                    originalBody,
                    context.RequestAborted);
            }
            finally
            {
                context.Response.Body = originalBody;
            }

            return;
        }

        await _next(context);

        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsDelete(context.Request.Method))
        {
            if (context.Response.StatusCode >= 200 &&
                context.Response.StatusCode < 300)
            {
                _keyRegistry.Clear(_cache);
            }
        }
    }

    private sealed record CachedResponse(
        int StatusCode,
        string? ContentType,
        byte[] Body);
}

public class CatalogCacheKeyRegistry
{
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public void Register(string key)
    {
        _keys.TryAdd(key, 0);
    }

    public void Clear(IMemoryCache cache)
    {
        foreach (var key in _keys.Keys)
        {
            cache.Remove(key);
        }

        _keys.Clear();
    }
}
