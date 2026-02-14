using System.Collections.Concurrent;
using Discord;
using Discord.Commands;
using ConsoleApp4.Configuration;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ConsoleApp4.Commands.General;

public sealed class ImgCommand : CommandBase
{
    private readonly PinterestImageSearchService _pinterest;
    private readonly ILogger<ImgCommand> _logger;

    public ImgCommand(
        PinterestImageSearchService pinterest,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService,
        ILogger<ImgCommand> logger)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _pinterest = pinterest;
        _logger = logger;
        ImgSessionStore.Configure(embedHelper);
    }

    [Command("img")]
    [Alias("pimg", "pinterest")]
    [Summary("Busca imagens na internet com filtro de conteudo adulto. Ex: *img anime girl")]
    public async Task ImgAsync([Remainder] string? query = null)
    {
        await TrackUserAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            await ReplyAsync($"Use: {Config.Value.Prefix}img <conteudo>");
            return;
        }

        var loading = await ReplyAsync("Buscando imagens na internet...");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var rawResults = await _pinterest.SearchAsync(query, maxResults: 100, cts.Token);
            var results = rawResults
                .Where(r => !string.IsNullOrWhiteSpace(NormalizeHttpUrl(r.ImageUrl, 1800)))
                .Take(100)
                .ToList();

            if (results.Count == 0)
            {
                await loading.ModifyAsync(msg =>
                {
                    msg.Content = "Nao encontrei resultados para esse termo.";
                    msg.Components = new ComponentBuilder().Build();
                    msg.Embeds = Array.Empty<Embed>();
                });
                return;
            }

            var token = ImgSessionStore.Create(Context.User.Id, query, results);
            var components = BuildComponents(token, ownerUserId: Context.User.Id, page: 1, query, results);
            try
            {
                // Components V2 cannot be mixed with content; also avoid converting the legacy
                // loading message into a V2 message via ModifyAsync.
                await ReplyAsync(components: components);
                await loading.ModifyAsync(msg =>
                {
                    msg.Content = "Resultados enviados.";
                    msg.Components = new ComponentBuilder().Build();
                    msg.Embeds = Array.Empty<Embed>();
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to modify loading message for img command. User={UserId}", Context.User.Id);
                try
                {
                    await ReplyAsync(components: components);
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "Failed to send img result components. User={UserId}", Context.User.Id);
                }

                await loading.ModifyAsync(msg =>
                {
                    msg.Content = "Resultados enviados.";
                    msg.Components = new ComponentBuilder().Build();
                    msg.Embeds = Array.Empty<Embed>();
                });
            }
        }
        catch (OperationCanceledException)
        {
            await loading.ModifyAsync(msg =>
            {
                msg.Content = "A busca demorou demais. Tente um termo mais especifico.";
                msg.Components = new ComponentBuilder().Build();
                msg.Embeds = Array.Empty<Embed>();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Img command failed. Query={Query} User={UserId}", query, Context.User.Id);
            await loading.ModifyAsync(msg =>
            {
                msg.Content = "Falha ao buscar imagens agora. Tente novamente em alguns segundos.";
                msg.Components = new ComponentBuilder().Build();
                msg.Embeds = Array.Empty<Embed>();
            });
        }
    }

    internal static MessageComponent BuildComponents(
        string token,
        ulong ownerUserId,
        int page,
        string query,
        IReadOnlyList<PinterestImageResult> results)
    {
        var total = results.Count;
        var totalPages = Math.Max(1, total);
        var safePage = Math.Clamp(page, 1, totalPages);
        var item = results[safePage - 1];

        var safeImageUrl = NormalizeHttpUrl(item.ImageUrl, 1800);
        var safeSourceUrl = NormalizeHttpUrl(item.PinUrl, 450);

        var title = "Busca de Imagens";
        var description = $"Busca: **{query}**\nResultado {safePage}/{totalPages}\nTotal coletado: **{total}** (max 100).";
        if (!string.IsNullOrWhiteSpace(item.Title))
        {
            var shortTitle = item.Title.Length > 120 ? item.Title[..120] : item.Title;
            description += $"\nTitulo: {shortTitle}";
        }

        if (!string.IsNullOrWhiteSpace(safeSourceUrl))
        {
            description += "\nFonte disponivel no botao abaixo.";
        }

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(Discord.Color.Orange)
            .WithCurrentTimestamp();

        if (!string.IsNullOrWhiteSpace(safeImageUrl))
        {
            embed.WithImageUrl(safeImageUrl);
        }

        var helper = ImgSessionStore.GetEmbedHelper();
        return helper.BuildCv2Card(embed, c =>
        {
            var row = new List<ButtonBuilder>
            {
                new ButtonBuilder()
                    .WithLabel("Anterior")
                    .WithCustomId($"img:prev:{token}:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage <= 1),
                new ButtonBuilder()
                    .WithLabel("Proxima")
                    .WithCustomId($"img:next:{token}:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage >= totalPages)
            };

            if (!string.IsNullOrWhiteSpace(safeSourceUrl))
            {
                row.Add(new ButtonBuilder()
                    .WithLabel("Abrir fonte")
                    .WithStyle(ButtonStyle.Link)
                    .WithUrl(safeSourceUrl));
            }

            c.WithActionRow(row);
        });
    }

    private static string? NormalizeHttpUrl(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var cleaned = input.Trim().Replace(" ", "%20");
        if (cleaned.Length > maxLength)
        {
            return null;
        }

        if (!Uri.TryCreate(cleaned, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        var absolute = uri.AbsoluteUri;
        if (absolute.Length > maxLength)
        {
            return null;
        }

        return absolute;
    }

    internal static class ImgSessionStore
    {
        private static readonly ConcurrentDictionary<string, ImgSession> Sessions = new(StringComparer.Ordinal);
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(2);
        private static EmbedHelper? _embedHelper;

        public static string Create(ulong ownerUserId, string query, IReadOnlyList<PinterestImageResult> results)
        {
            CleanupExpired();
            var token = Guid.NewGuid().ToString("N")[..10];
            Sessions[token] = new ImgSession(ownerUserId, query, results, DateTime.UtcNow);
            return token;
        }

        public static bool TryGet(string token, out ImgSession session)
        {
            session = null!;
            if (!Sessions.TryGetValue(token, out var found))
            {
                return false;
            }

            if (DateTime.UtcNow - found.CreatedAtUtc > Ttl)
            {
                Sessions.TryRemove(token, out _);
                return false;
            }

            session = found;
            return true;
        }

        public static void Configure(EmbedHelper helper)
        {
            _embedHelper = helper;
        }

        public static EmbedHelper GetEmbedHelper()
        {
            return _embedHelper ?? throw new InvalidOperationException("ImgSessionStore not configured.");
        }

        private static void CleanupExpired()
        {
            var threshold = DateTime.UtcNow - Ttl;
            foreach (var pair in Sessions)
            {
                if (pair.Value.CreatedAtUtc < threshold)
                {
                    Sessions.TryRemove(pair.Key, out _);
                }
            }
        }
    }

    internal sealed record ImgSession(
        ulong OwnerUserId,
        string Query,
        IReadOnlyList<PinterestImageResult> Results,
        DateTime CreatedAtUtc);
}
