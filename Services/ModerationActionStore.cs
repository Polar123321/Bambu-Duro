using Microsoft.Extensions.Caching.Memory;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class ModerationActionStore : IModerationActionStore
{
    private readonly IMemoryCache _cache;

    public ModerationActionStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Add(ModerationAction action, TimeSpan ttl)
    {
        var token = action.Token;
        _cache.Set(token, action, ttl);
        return token;
    }

    public bool TryGet(string token, out ModerationAction action)
    {
        if (_cache.TryGetValue(token, out ModerationAction? stored) && stored != null)
        {
            action = stored;
            return true;
        }

        action = null!;
        return false;
    }

    public void Remove(string token)
    {
        _cache.Remove(token);
    }
}
