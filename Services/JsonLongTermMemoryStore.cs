using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class JsonLongTermMemoryStore : ILongTermMemoryStore
{
    private sealed class LongUserMemoryFile
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Facts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<LongMemoryMessage> Messages { get; set; } = new();
    }

    private sealed class LongMemoryMessage
    {
        public ulong ChannelId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly Regex NameRegex = new(
        @"\b(?:meu nome e|me chama|me chamo)\s+([a-zA-Z0-9_]{2,24})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AgeRegex = new(
        @"\btenho\s+(\d{1,2})\s+anos\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LikesRegex = new(
        @"\b(?:eu gosto de|gosto de|curto|prefiro)\s+(.{2,70})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LocationRegex = new(
        @"\b(?:sou de|moro em)\s+(.{2,60})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WorkRegex = new(
        @"\btrabalho com\s+(.{2,60})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "para", "porque", "sobre", "isso", "essa", "esse", "aqui", "ali", "tipo", "cara", "mano",
        "como", "onde", "quando", "muito", "pouco", "nada", "tudo", "com", "sem"
    };

    private static readonly HashSet<string> TrivialMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        "oi", "ola", "opa", "eae", "iae", "salve", "bom dia", "boa tarde", "boa noite",
        "ok", "blz", "beleza", "vlw", "valeu", "kk", "kkk", "k", "rs", "rsrs", "hm", "hmm", "sla",
        "...", ".", "..", "nada", "nada nao", "nada não"
    };

    private static readonly SemaphoreSlim IoLock = new(1, 1);

    private readonly IOptions<BrainConfiguration> _cfg;
    private readonly ILogger<JsonLongTermMemoryStore> _logger;

    public JsonLongTermMemoryStore(
        IOptions<BrainConfiguration> cfg,
        ILogger<JsonLongTermMemoryStore> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    public async Task AppendMessageAsync(
        ulong guildId,
        ulong channelId,
        ulong userId,
        string username,
        string content,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!_cfg.Value.LongJsonMemoryEnabled)
        {
            return;
        }

        var normalized = Normalize(content);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        var filePath = GetUserFilePath(guildId, userId);
        await IoLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var doc = await ReadFileAsync(filePath, cancellationToken).ConfigureAwait(false)
                      ?? new LongUserMemoryFile
                      {
                          GuildId = guildId,
                          UserId = userId
                      };

            doc.Username = string.IsNullOrWhiteSpace(username) ? doc.Username : username.Trim();
            doc.UpdatedAtUtc = DateTime.UtcNow;
            doc.Messages.Add(new LongMemoryMessage
            {
                ChannelId = channelId,
                CreatedAtUtc = createdAtUtc,
                Content = normalized
            });

            ExtractFacts(doc.Facts, normalized);
            TrimOverflow(doc.Messages);

            await WriteFileAsync(filePath, doc, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IoLock.Release();
        }
    }

    public async Task<LongTermMemoryContext> GetContextAsync(
        ulong guildId,
        ulong userId,
        string? currentPrompt = null,
        int? maxMessages = null,
        CancellationToken cancellationToken = default)
    {
        if (!_cfg.Value.LongJsonMemoryEnabled)
        {
            return LongTermMemoryContext.Empty;
        }

        var filePath = GetUserFilePath(guildId, userId);
        await IoLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var doc = await ReadFileAsync(filePath, cancellationToken).ConfigureAwait(false);
            if (doc == null || doc.Messages.Count == 0)
            {
                return LongTermMemoryContext.Empty;
            }

            var take = Math.Clamp(maxMessages ?? _cfg.Value.LongJsonContextMessages, 4, 200);
            var relevant = SelectRelevantMessages(doc.Messages, currentPrompt, take);

            var facts = doc.Facts
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"{kv.Key}: {kv.Value}")
                .ToList();

            return new LongTermMemoryContext
            {
                TotalMessagesStored = doc.Messages.Count,
                Facts = facts,
                RelevantMessages = relevant
            };
        }
        finally
        {
            IoLock.Release();
        }
    }

    private string GetUserFilePath(ulong guildId, ulong userId)
    {
        var directory = ResolveBaseDirectory();
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"{guildId}_{userId}.json");
    }

    private string ResolveBaseDirectory()
    {
        var configured = (_cfg.Value.LongJsonDirectory ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "long-memory";
        }

        if (Path.IsPathRooted(configured))
        {
            return configured;
        }

        var stableRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ConsoleApp4");
        return Path.Combine(stableRoot, configured.Replace('/', Path.DirectorySeparatorChar));
    }

    private async Task<LongUserMemoryFile?> ReadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            await using var fs = File.OpenRead(filePath);
            var doc = await JsonSerializer.DeserializeAsync<LongUserMemoryFile>(fs, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return doc;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read long memory file {FilePath}", filePath);
            return null;
        }
    }

    private static async Task WriteFileAsync(string filePath, LongUserMemoryFile doc, CancellationToken cancellationToken)
    {
        var tempFile = filePath + ".tmp";
        await using (var fs = File.Create(tempFile))
        {
            await JsonSerializer.SerializeAsync(fs, doc, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempFile, filePath, overwrite: true);
    }

    private void TrimOverflow(List<LongMemoryMessage> messages)
    {
        var max = Math.Max(500, _cfg.Value.LongJsonMaxMessagesPerUser);
        if (messages.Count <= max)
        {
            return;
        }

        var remove = messages.Count - max;
        messages.RemoveRange(0, remove);
    }

    private static List<UserMemoryMessageContext> SelectRelevantMessages(
        IReadOnlyList<LongMemoryMessage> allMessages,
        string? currentPrompt,
        int take)
    {
        var cleaned = allMessages
            .Where(m => !IsLowValue(m.Content))
            .ToList();
        if (cleaned.Count == 0)
        {
            return new List<UserMemoryMessageContext>();
        }

        var recent = cleaned
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(Math.Min(take, 14))
            .ToList();

        var promptTerms = ExtractTerms(currentPrompt);
        var related = promptTerms.Count == 0
            ? new List<LongMemoryMessage>()
            : cleaned
                .Where(m => ContainsAnyTerm(m.Content, promptTerms))
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(Math.Min(take, 12))
                .ToList();

        var merged = related
            .Concat(recent)
            .GroupBy(m => $"{m.CreatedAtUtc:O}|{m.Content}", StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(m => m.CreatedAtUtc)
            .TakeLast(take)
            .Select(m => new UserMemoryMessageContext
            {
                CreatedAtUtc = m.CreatedAtUtc,
                Content = m.Content,
                MoralTag = "neutro"
            })
            .ToList();

        return merged;
    }

    private static bool ContainsAnyTerm(string content, HashSet<string> terms)
    {
        var lower = content.ToLowerInvariant();
        foreach (var term in terms)
        {
            if (lower.Contains(term, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> ExtractTerms(string? text)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return terms;
        }

        var parts = Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(p => p.Length >= 4);
        foreach (var part in parts)
        {
            if (StopWords.Contains(part))
            {
                continue;
            }

            terms.Add(part);
        }

        return terms;
    }

    private static void ExtractFacts(IDictionary<string, string> facts, string text)
    {
        TryFact("nome", NameRegex, text, facts);
        TryFact("idade", AgeRegex, text, facts, " anos");
        TryFact("gostos", LikesRegex, text, facts);
        TryFact("local", LocationRegex, text, facts);
        TryFact("trabalho", WorkRegex, text, facts);
    }

    private static void TryFact(
        string key,
        Regex regex,
        string text,
        IDictionary<string, string> facts,
        string suffix = "")
    {
        var match = regex.Match(text);
        if (!match.Success || match.Groups.Count < 2)
        {
            return;
        }

        var value = Clean(match.Groups[1].Value);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(suffix) && !value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            value += suffix;
        }

        facts[key] = value;
    }

    private static string Normalize(string content)
    {
        var text = (content ?? string.Empty)
            .Replace("\u200B", string.Empty)
            .Replace("\u200C", string.Empty)
            .Replace("\u200D", string.Empty)
            .Replace("\uFEFF", string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        while (text.Contains("  ", StringComparison.Ordinal))
        {
            text = text.Replace("  ", " ", StringComparison.Ordinal);
        }

        if (text.Length > 1800)
        {
            text = text[..1800];
        }

        return text;
    }

    private static string Clean(string value)
    {
        var cleaned = value.Trim().Trim('.', ',', ';', ':', '!', '?', '"', '\'', ')', '(');
        if (cleaned.Length > 80)
        {
            cleaned = cleaned[..80].TrimEnd();
        }

        return cleaned;
    }

    private static bool IsLowValue(string content)
    {
        var normalized = Normalize(content).ToLowerInvariant();
        if (normalized.Length < 6)
        {
            return true;
        }

        return TrivialMessages.Contains(normalized);
    }
}
