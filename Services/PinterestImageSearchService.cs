using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class PinterestImageSearchService
{
    private static readonly string[] AdultKeywordFragments =
    {
        "porn", "porno", "xxx", "xvideos", "xvideo", "pornhub", "redtube", "xhamster",
        "youporn", "brazzers", "onlyfans", "nhentai", "hentai", "nsfw", "rule34", "r34",
        "gelbooru", "danbooru", "sankaku", "e621", "e926", "fakku", "xhamster", "xnxx",
        "xnxx", "youjizz", "jav", "sex", "nude", "naked", "erotic", "boobs", "pussy",
        "anal", "blowjob", "fap", "milf", "incest", "doujin", "ecchi", "lewd"
    };
    private static readonly HashSet<string> AdultTokenKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "porn", "porno", "xxx", "pornhub", "xvideos", "redtube", "xhamster", "youporn",
        "nhentai", "hentai", "nsfw", "rule34", "r34", "sex", "sexo", "nude", "naked",
        "erotic", "boobs", "pussy", "anal", "blowjob", "ecchi", "lewd", "doujin"
    };
    private static readonly string[] AdultBlockedDomains =
    {
        "pornhub.com", "xvideos.com", "xhamster.com", "redtube.com", "youporn.com",
        "xnxx.com", "nhentai.net", "rule34.xxx", "gelbooru.com", "danbooru.donmai.us",
        "sankakucomplex.com", "e621.net", "e926.net", "fakku.net", "hentai-foundry.com",
        "hentaifox.com", "hanime.tv", "hentaihaven.xxx", "hentaicore.net"
    };
    private static readonly Regex NonWordRegex = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PinUrlRegex = new(
        "https?://(?:[a-z]{2}\\.)?pinterest\\.[^/]+/pin/([0-9]{6,})/?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PinImgRegex = new(
        "https://i\\.pinimg\\.com/[^\\s\\]\\[\\)\\(\"'<>]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BingMetadataRegex = new(
        "m=\"\\{(?<data>.*?)\\}\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PinterestImageSearchService> _logger;

    public PinterestImageSearchService(HttpClient http, IMemoryCache cache, ILogger<PinterestImageSearchService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(20);
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }
    }

    public async Task<IReadOnlyList<PinterestImageResult>> SearchAsync(string query, int maxResults = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = NormalizeQuery(query);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Array.Empty<PinterestImageResult>();
            }
            if (!IsSafeQuery(normalized))
            {
                return Array.Empty<PinterestImageResult>();
            }

            var safeMax = Math.Clamp(maxResults, 1, 100);
            var cacheKey = $"pinterest:search:{safeMax}:{normalized.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out List<PinterestImageResult>? cached) && cached != null)
            {
                return cached;
            }

            var results = await DiscoverFromBingAsync(normalized, safeMax, cancellationToken).ConfigureAwait(false);
            if (results.Count < Math.Min(12, safeMax))
            {
                var pinUrls = await DiscoverPinUrlsAsync(normalized, cancellationToken).ConfigureAwait(false);
                var fromPins = await HydratePinsAsync(pinUrls, safeMax, cancellationToken).ConfigureAwait(false);
                results = MergeResults(results, fromPins, safeMax);
            }

            if (results.Count == 0)
            {
                results = await DiscoverLooseImagesAsync(normalized, safeMax, cancellationToken).ConfigureAwait(false);
            }

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(8));
            return results;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image search pipeline failed for query '{Query}'", query);
            return Array.Empty<PinterestImageResult>();
        }
    }

    private async Task<IReadOnlyList<string>> DiscoverPinUrlsAsync(string query, CancellationToken cancellationToken)
    {
        var variants = BuildQueryVariants(query);
        var targets = new List<string>();

        foreach (var q in variants)
        {
            var encoded = Uri.EscapeDataString(q);
            targets.Add($"https://r.jina.ai/http://br.pinterest.com/search/pins/?q={encoded}&rs=typed");
            targets.Add($"https://r.jina.ai/http://br.pinterest.com/search/pins/?q={encoded}&rs=ac");
        }

        var all = new ConcurrentBag<string>();
        using var gate = new SemaphoreSlim(4);
        var tasks = targets.Select(async url =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var text = await _http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
                foreach (var pin in ExtractPinUrls(text))
                {
                    all.Add(pin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Pinterest discovery request failed for {Url}", url);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return all.Distinct(StringComparer.OrdinalIgnoreCase).Take(140).ToList();
    }

    private async Task<IReadOnlyList<PinterestImageResult>> HydratePinsAsync(
        IReadOnlyList<string> pinUrls,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentBag<PinterestImageResult>();
        using var gate = new SemaphoreSlim(6);

        var tasks = pinUrls.Select(async pinUrl =>
        {
            if (results.Count >= maxResults)
            {
                return;
            }

            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var readerUrl = $"https://r.jina.ai/http://{pinUrl.TrimStart('/')}";
                var text = await _http.GetStringAsync(readerUrl, cancellationToken).ConfigureAwait(false);
                var imageUrl = NormalizeUrl(ExtractBestImageUrl(text));
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return;
                }

                var title = ExtractTitle(text);
                results.Add(new PinterestImageResult(imageUrl, NormalizeUrl(pinUrl) ?? pinUrl, title));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Pinterest pin hydrate failed for {PinUrl}", pinUrl);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return results
            .GroupBy(r => r.ImageUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(maxResults)
            .ToList();
    }

    private async Task<IReadOnlyList<PinterestImageResult>> DiscoverLooseImagesAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var variants = BuildQueryVariants(query).ToList();
        var targets = new List<(string SearchUrl, string ReaderUrl)>();
        foreach (var q in variants)
        {
            var encoded = Uri.EscapeDataString(q);
            var searchUrl = $"https://br.pinterest.com/search/pins/?q={encoded}&rs=typed";
            targets.Add((searchUrl, $"https://r.jina.ai/http://{searchUrl}"));
            searchUrl = $"https://br.pinterest.com/search/pins/?q={encoded}&rs=ac";
            targets.Add((searchUrl, $"https://r.jina.ai/http://{searchUrl}"));
        }

        var all = new ConcurrentBag<PinterestImageResult>();
        using var gate = new SemaphoreSlim(4);
        var tasks = targets.Select(async t =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var text = await _http.GetStringAsync(t.ReaderUrl, cancellationToken).ConfigureAwait(false);
                var urls = PinImgRegex.Matches(text)
                    .Select(m => CleanupUrl(m.Value))
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(u => ScoreImageUrl(u!))
                    .Take(80)
                    .ToList();

                foreach (var imageUrl in urls)
                {
                    var safeImage = NormalizeUrl(imageUrl);
                    if (!string.IsNullOrWhiteSpace(safeImage))
                    {
                        all.Add(new PinterestImageResult(safeImage, t.SearchUrl, null));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Pinterest loose image request failed for {Url}", t.ReaderUrl);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return all
            .GroupBy(x => x.ImageUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Where(IsSafe)
            .OrderByDescending(x => ScoreImageUrl(x.ImageUrl))
            .Take(maxResults)
            .ToList();
    }

    private async Task<IReadOnlyList<PinterestImageResult>> DiscoverFromBingAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var collected = new List<PinterestImageResult>(capacity: Math.Min(100, maxResults));
        var encoded = Uri.EscapeDataString(query);

        for (var first = 0; first < 140 && collected.Count < maxResults; first += 35)
        {
            var url = $"https://www.bing.com/images/async?q={encoded}&first={first}&count=35&adlt=strict&mmasync=1&layout=RowBased";
            string html;
            try
            {
                html = await _http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Bing image request failed for {Query} first={First}", query, first);
                continue;
            }

            var pageItems = ParseBingResults(html);
            if (pageItems.Count == 0)
            {
                continue;
            }

            collected.AddRange(pageItems);
        }

        return collected
            .GroupBy(x => x.ImageUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Where(IsSafe)
            .Take(maxResults)
            .ToList();
    }

    private static IReadOnlyList<PinterestImageResult> ParseBingResults(string html)
    {
        var list = new List<PinterestImageResult>();
        foreach (Match match in BingMetadataRegex.Matches(html))
        {
            if (!match.Success)
            {
                continue;
            }

            var rawData = match.Groups["data"].Value;
            if (string.IsNullOrWhiteSpace(rawData))
            {
                continue;
            }

            var decoded = WebUtility.HtmlDecode("{" + rawData + "}");
            if (string.IsNullOrWhiteSpace(decoded))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(decoded);
                var root = doc.RootElement;
                var imageUrl = NormalizeUrl(GetString(root, "murl"));
                var pageUrl = NormalizeUrl(GetString(root, "purl"));
                var title = GetString(root, "t");
                if (!IsValidUrl(imageUrl) || !IsValidUrl(pageUrl))
                {
                    continue;
                }

                list.Add(new PinterestImageResult(imageUrl!, pageUrl!, title));
            }
            catch
            {
                
            }
        }

        return list;
    }

    private static string? GetString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static bool IsValidUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string? NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(value.Trim()).Replace(" ", "%20");
        if (!Uri.TryCreate(decoded, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        return uri.AbsoluteUri;
    }

    private static IReadOnlyList<PinterestImageResult> MergeResults(
        IReadOnlyList<PinterestImageResult> primary,
        IReadOnlyList<PinterestImageResult> secondary,
        int maxResults)
    {
        return primary
            .Concat(secondary)
            .GroupBy(x => x.ImageUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Where(IsSafe)
            .Take(maxResults)
            .ToList();
    }

    private static bool IsSafe(PinterestImageResult result)
    {
        var text = $"{result.ImageUrl} {result.PinUrl} {result.Title}";
        return !ContainsAdultKeyword(text) &&
               !HasBlockedDomain(result.ImageUrl) &&
               !HasBlockedDomain(result.PinUrl);
    }

    private static bool IsSafeQuery(string query)
    {
        return !ContainsAdultKeyword(query) && !HasBlockedDomain(query);
    }

    private static bool ContainsAdultKeyword(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var lower = value.ToLowerInvariant();
        foreach (var fragment in AdultKeywordFragments)
        {
            if (lower.Contains(fragment, StringComparison.Ordinal))
            {
                return true;
            }
        }

        var tokens = NonWordRegex.Split(lower);
        foreach (var token in tokens)
        {
            if (token.Length == 0)
            {
                continue;
            }

            if (AdultTokenKeywords.Contains(token))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasBlockedDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        foreach (var blocked in AdultBlockedDomains)
        {
            if (host == blocked || host.EndsWith("." + blocked, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> BuildQueryVariants(string query)
    {
        var list = new[]
        {
            query,
            $"{query} wallpaper",
            $"{query} art",
            $"{query} aesthetic",
            $"{query} icon",
            $"{query} pfp"
        };

        return list
            .Select(NormalizeQuery)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExtractPinUrls(string text)
    {
        foreach (Match match in PinUrlRegex.Matches(text))
        {
            if (!match.Success)
            {
                continue;
            }

            var pinId = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(pinId))
            {
                continue;
            }

            yield return $"https://br.pinterest.com/pin/{pinId}/";
        }
    }

    private static string? ExtractBestImageUrl(string text)
    {
        var urls = PinImgRegex.Matches(text)
            .Select(m => CleanupUrl(m.Value))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (urls.Count == 0)
        {
            return null;
        }

        var preferred = urls
            .OrderByDescending(u => ScoreImageUrl(u!))
            .FirstOrDefault();

        return preferred;
    }

    private static string? ExtractTitle(string text)
    {
        
        var marker = "Title:";
        var idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        var start = idx + marker.Length;
        var end = text.IndexOf('\n', start);
        if (end < 0)
        {
            end = Math.Min(text.Length, start + 180);
        }

        var value = text[start..end].Trim();
        if (value.Length == 0)
        {
            return null;
        }

        return value.Length <= 120 ? value : value[..120];
    }

    private static int ScoreImageUrl(string url)
    {
        var score = 0;
        if (url.Contains("/originals/", StringComparison.OrdinalIgnoreCase)) score += 500;
        if (url.Contains("/736x/", StringComparison.OrdinalIgnoreCase)) score += 300;
        if (url.Contains("/564x/", StringComparison.OrdinalIgnoreCase)) score += 200;
        if (url.Contains("_RS", StringComparison.OrdinalIgnoreCase)) score -= 100;
        if (url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) score += 20;
        if (url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) score += 10;
        return score;
    }

    private static string CleanupUrl(string value)
    {
        var clean = value.Trim().TrimEnd('.', ',', ';', ')', ']', '>');
        return clean;
    }

    private static string NormalizeQuery(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', parts);
    }
}
