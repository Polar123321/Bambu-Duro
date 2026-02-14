using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using ConsoleApp4.Data;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class UserMemoryService : IUserMemoryService
{
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

    private static readonly HashSet<string> TrivialContextMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        "oi", "ola", "opa", "eae", "iae", "salve", "bom dia", "boa tarde", "boa noite",
        "ok", "blz", "beleza", "vlw", "valeu", "kk", "kkk", "k", "rs", "rsrs", "hm", "hmm", "sla",
        "nada", "nada nao", "nada não"
    };

    private readonly BotDbContext _db;
    private readonly IOptions<BrainConfiguration> _cfg;
    private readonly ILongTermMemoryStore _longMemory;

    public UserMemoryService(BotDbContext db, IOptions<BrainConfiguration> cfg, ILongTermMemoryStore longMemory)
    {
        _db = db;
        _cfg = cfg;
        _longMemory = longMemory;
    }

    public bool ShouldTrackUser(ulong userId)
    {
        var cfg = _cfg.Value;
        if (!cfg.Enabled)
        {
            return false;
        }

        if (cfg.TrackedUserIds == null || cfg.TrackedUserIds.Count == 0)
        {
            return true;
        }

        return cfg.TrackedUserIds.Contains(userId);
    }

    public async Task CaptureMessageAsync(
        ulong guildId,
        ulong channelId,
        ulong userId,
        string username,
        string content,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldTrackUser(userId))
        {
            return;
        }

        var normalized = Normalize(content);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        var safeUsername = string.IsNullOrWhiteSpace(username) ? $"user-{userId}" : username.Trim();
        if (safeUsername.Length > 64)
        {
            safeUsername = safeUsername[..64];
        }

        var moralTag = "neutro";

        _db.UserMemoryEntries.Add(new UserMemoryEntry
        {
            Id = Guid.NewGuid(),
            DiscordGuildId = guildId,
            DiscordChannelId = channelId,
            DiscordUserId = userId,
            Username = safeUsername,
            Content = normalized,
            MoralTag = moralTag,
            CreatedAtUtc = createdAtUtc
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await TrimOverflowAsync(guildId, userId, cancellationToken).ConfigureAwait(false);
        await TrimExpiredAsync(cancellationToken).ConfigureAwait(false);

        await _longMemory.AppendMessageAsync(
            guildId,
            channelId,
            userId,
            safeUsername,
            normalized,
            createdAtUtc,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserMemoryContext> GetContextAsync(
        ulong guildId,
        ulong userId,
        string? currentPrompt = null,
        int? maxMessages = null,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldTrackUser(userId))
        {
            return UserMemoryContext.Empty(guildId, userId);
        }

        var cfg = _cfg.Value;
        var take = Math.Clamp(maxMessages ?? cfg.MaxContextMessages, 1, 24);

        var recent = await _db.UserMemoryEntries
            .AsNoTracking()
            .Where(m => m.DiscordGuildId == guildId && m.DiscordUserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var longContext = await _longMemory.GetContextAsync(
            guildId,
            userId,
            currentPrompt,
            _cfg.Value.LongJsonContextMessages,
            cancellationToken).ConfigureAwait(false);

        if (recent.Count == 0 && longContext.TotalMessagesStored == 0)
        {
            return UserMemoryContext.Empty(guildId, userId);
        }

        var allForFacts = await _db.UserMemoryEntries
            .AsNoTracking()
            .Where(m => m.DiscordGuildId == guildId && m.DiscordUserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(Math.Max(40, _cfg.Value.MaxMessagesPerUser))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var stats = await _db.UserMemoryEntries
            .AsNoTracking()
            .Where(m => m.DiscordGuildId == guildId && m.DiscordUserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeline = recent
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new UserMemoryMessageContext
            {
                CreatedAtUtc = m.CreatedAtUtc,
                Content = m.Content,
                MoralTag = m.MoralTag
            })
            .ToList();

        var total = stats?.Total ?? recent.Count;
        var moralSummary = "Moralidade desativada.";

        return new UserMemoryContext
        {
            DiscordGuildId = guildId,
            DiscordUserId = userId,
            TotalMessagesStored = total,
            PositiveCount = 0,
            NeutralCount = total,
            QuestionableCount = 0,
            LongJsonMessagesStored = longContext.TotalMessagesStored,
            UserFacts = ExtractFacts(allForFacts),
            LongJsonFacts = longContext.Facts,
            RecentMessages = timeline,
            LongJsonRelevantMessages = longContext.RelevantMessages,
            MoralSummary = moralSummary
        };
    }

    public string BuildPromptContext(UserMemoryContext context)
    {
        if (context.RecentMessages.Count == 0 && context.LongJsonRelevantMessages.Count == 0)
        {
            return "Sem memoria salva para este usuario.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Contexto interno do usuario:");

        if (context.UserFacts.Count > 0)
        {
            sb.AppendLine("- fatos estaveis:");
            foreach (var fact in context.UserFacts)
            {
                sb.Append("  - ");
                sb.AppendLine(fact);
            }
        }

        if (context.LongJsonMessagesStored > 0)
        {
            sb.AppendLine($"- memoria longa json: {context.LongJsonMessagesStored} mensagens salvas");
        }

        if (context.LongJsonFacts.Count > 0)
        {
            sb.AppendLine("- fatos estaveis (json):");
            foreach (var fact in context.LongJsonFacts.Take(12))
            {
                sb.Append("  - ");
                sb.AppendLine(fact);
            }
        }

        var relevantMessages = context.RecentMessages
            .Where(m => !IsLowValueContext(m.Content))
            .TakeLast(10)
            .ToList();

        if (relevantMessages.Count > 0)
        {
            sb.AppendLine("- mensagens recentes relevantes:");
            foreach (var item in relevantMessages)
            {
                sb.Append("  - [");
                sb.Append(item.CreatedAtUtc.ToString("HH:mm"));
                sb.Append("] ");
                sb.AppendLine(ShortenForPrompt(item.Content, 180));
            }
        }

        if (context.LongJsonRelevantMessages.Count > 0)
        {
            sb.AppendLine("- lembrancas antigas relevantes (json):");
            foreach (var item in context.LongJsonRelevantMessages.TakeLast(10))
            {
                sb.Append("  - [");
                sb.Append(item.CreatedAtUtc.ToString("MM-dd HH:mm"));
                sb.Append("] ");
                sb.AppendLine(ShortenForPrompt(item.Content, 160));
            }
        }

        sb.AppendLine("- instrucoes:");
        sb.AppendLine("  - use o contexto apenas se ajudar na mensagem atual");
        sb.AppendLine("  - nao repita frases literais do usuario");
        sb.AppendLine("  - nao mencione que esta lembrando explicitamente");
        return sb.ToString().Trim();
    }

    private static IReadOnlyList<string> ExtractFacts(IReadOnlyList<UserMemoryEntry> entries)
    {
        if (entries.Count == 0)
        {
            return Array.Empty<string>();
        }

        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries.OrderBy(e => e.CreatedAtUtc))
        {
            TryExtractFact("nome", NameRegex, entry.Content, facts, clean: true);
            TryExtractFact("idade", AgeRegex, entry.Content, facts, clean: false, suffix: " anos");
            TryExtractFact("gostos", LikesRegex, entry.Content, facts, clean: true);
            TryExtractFact("local", LocationRegex, entry.Content, facts, clean: true);
            TryExtractFact("trabalho", WorkRegex, entry.Content, facts, clean: true);
        }

        var result = new List<string>(capacity: 5);
        if (facts.TryGetValue("nome", out var nome))
        {
            result.Add($"nome: {nome}");
        }
        if (facts.TryGetValue("idade", out var idade))
        {
            result.Add($"idade: {idade}");
        }
        if (facts.TryGetValue("gostos", out var gostos))
        {
            result.Add($"gosta de: {gostos}");
        }
        if (facts.TryGetValue("local", out var local))
        {
            result.Add($"local: {local}");
        }
        if (facts.TryGetValue("trabalho", out var trabalho))
        {
            result.Add($"trabalho/area: {trabalho}");
        }

        return result;
    }

    private static void TryExtractFact(
        string key,
        Regex regex,
        string text,
        IDictionary<string, string> target,
        bool clean,
        string? suffix = null)
    {
        var match = regex.Match(text);
        if (!match.Success || match.Groups.Count < 2)
        {
            return;
        }

        var value = match.Groups[1].Value.Trim();
        if (clean)
        {
            value = CleanFactValue(value);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(suffix) && !value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            value += suffix;
        }

        target[key] = value;
    }

    private static string CleanFactValue(string value)
    {
        var cleaned = value.Trim();
        cleaned = cleaned.Trim('.', ',', ';', ':', '!', '?', '"', '\'', ')', '(');
        if (cleaned.Length > 70)
        {
            cleaned = cleaned[..70].TrimEnd();
        }

        return cleaned;
    }

    private async Task TrimOverflowAsync(ulong guildId, ulong userId, CancellationToken cancellationToken)
    {
        var maxMessages = Math.Max(10, _cfg.Value.MaxMessagesPerUser);
        var overflowIds = await _db.UserMemoryEntries
            .Where(m => m.DiscordGuildId == guildId && m.DiscordUserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(maxMessages)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (overflowIds.Count == 0)
        {
            return;
        }

        var overflowRows = await _db.UserMemoryEntries
            .Where(m => overflowIds.Contains(m.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (overflowRows.Count == 0)
        {
            return;
        }

        _db.UserMemoryEntries.RemoveRange(overflowRows);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task TrimExpiredAsync(CancellationToken cancellationToken)
    {
        var retentionDays = _cfg.Value.RetentionDays;
        if (retentionDays <= 0)
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var staleRows = await _db.UserMemoryEntries
            .Where(m => m.CreatedAtUtc < cutoff)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(120)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (staleRows.Count == 0)
        {
            return;
        }

        _db.UserMemoryEntries.RemoveRange(staleRows);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string Normalize(string content)
    {
        var text = (content ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return string.Empty;
        }

        text = text
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

        if (text.Length > 1200)
        {
            text = text[..1200];
        }

        return text;
    }

    private static bool IsLowValueContext(string content)
    {
        var normalized = Normalize(content).ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return true;
        }

        if (TrivialContextMessages.Contains(normalized))
        {
            return true;
        }

        if (normalized.Length < 6)
        {
            return true;
        }

        return false;
    }

    private static string ShortenForPrompt(string text, int maxChars)
    {
        var normalized = Normalize(text);
        if (normalized.Length <= maxChars)
        {
            return normalized;
        }

        return normalized[..maxChars].TrimEnd() + "...";
    }
}
