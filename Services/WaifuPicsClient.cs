using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ConsoleApp4.Services;

public sealed class WaifuPicsClient
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WaifuPicsClient> _logger;
    private const int RecentMax = 6;

    public WaifuPicsClient(HttpClient http, IMemoryCache cache, ILogger<WaifuPicsClient> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetImageUrlAsync(string category, bool nsfw)
    {
        var scope = nsfw ? "nsfw" : "sfw";
        var url = $"https://api.waifu.pics/{scope}/{category}";
        var recentKey = $"waifu:recent:{scope}:{category}";
        var recent = _cache.GetOrCreate(recentKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return new Queue<string>();
        }) ?? new Queue<string>();

        try
        {
            for (var i = 0; i < 3; i++)
            {
                var response = await _http.GetFromJsonAsync<WaifuPicsResponse>(url);
                if (string.IsNullOrWhiteSpace(response?.Url))
                {
                    continue;
                }

                if (!recent.Contains(response.Url))
                {
                    var urlValue = response!.Url;
                    recent.Enqueue(urlValue);
                    while (recent.Count > RecentMax)
                    {
                        recent.Dequeue();
                    }
                    return urlValue;
                }
            }

            
            var fallback = await _http.GetFromJsonAsync<WaifuPicsResponse>(url);
            if (!string.IsNullOrWhiteSpace(fallback?.Url))
            {
                var urlValue = fallback!.Url;
                recent.Enqueue(urlValue);
                while (recent.Count > RecentMax)
                {
                    recent.Dequeue();
                }
                return urlValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch waifu.pics image for {Scope}/{Category}", scope, category);
        }

        return null;
    }

    private sealed class WaifuPicsResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
