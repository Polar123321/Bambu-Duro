using Microsoft.Extensions.Caching.Memory;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Services;

public sealed class RateLimitService : IRateLimitService
{
    private readonly IMemoryCache _cache;

    public RateLimitService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryConsume(ulong userId, string bucket, TimeSpan window)
    {
        return TryConsume(userId, bucket, window, out _);
    }

    public bool TryConsume(ulong userId, string bucket, TimeSpan window, out TimeSpan retryAfter)
    {
        var key = $"rate:{bucket}:{userId}";
        if (_cache.TryGetValue(key, out DateTimeOffset expiresAt))
        {
            var now = DateTimeOffset.UtcNow;
            retryAfter = expiresAt > now ? expiresAt - now : TimeSpan.Zero;
            return false;
        }

        var newExpires = DateTimeOffset.UtcNow.Add(window);
        _cache.Set(key, newExpires, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = newExpires
        });
        retryAfter = TimeSpan.Zero;
        return true;
    }
}
