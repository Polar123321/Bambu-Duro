using Discord.Commands;
using Discord;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services;
using ConsoleApp4.Services.Interfaces;
using System.Text.RegularExpressions;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.Fun;

public sealed class ActionCommands : CommandBase
{
    private static readonly string[] NsfwOnlyCategories =
    {
        "waifu",
        "neko",
        "trap",
        "blowjob"
    };

    private readonly WaifuPicsClient _waifu;
    private readonly IGuildConfigStore _configStore;
    private readonly IShipStore _shipStore;
    private readonly IShipCompatibilityService _ship;
    private readonly IMarriageStore _marriageStore;

    public ActionCommands(
        WaifuPicsClient waifu,
        IGuildConfigStore configStore,
        IShipStore shipStore,
        IShipCompatibilityService ship,
        IMarriageStore marriageStore,
        EmbedHelper embedHelper,
        Microsoft.Extensions.Options.IOptions<ConsoleApp4.Configuration.BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _waifu = waifu;
        _configStore = configStore;
        _shipStore = shipStore;
        _ship = ship;
        _marriageStore = marriageStore;
    }

    [Command("hug")]
    [Alias("abraco", "abraço")]
    [Summary("Envia um abraco.")]
    public async Task HugAsync([Remainder] string? targetInput = null)
    {
        await SendActionAsync("hug", "🤗 Abraco", targetInput);
    }

    [Command("kiss")]
    [Alias("beijo", "beijar")]
    [Summary("Envia um beijo.")]
    public async Task KissAsync([Remainder] string? targetInput = null)
    {
        await SendActionAsync("kiss", "💋 Beijo", targetInput, allowNsfw: true);
    }

    [Command("pat")]
    [Alias("cafune")]
    [Summary("Faz cafune.")]
    public async Task PatAsync([Remainder] string? targetInput = null)
    {
        await SendActionAsync("pat", "🫳 Cafune", targetInput);
    }

    [Command("cuddle")]
    [Alias("aconchego")]
    [Summary("Aconchega alguem.")]
    public async Task CuddleAsync([Remainder] string? targetInput = null)
    {
        await SendActionAsync("cuddle", "🫶 Aconchego", targetInput);
    }

    [Command("slap")]
    [Alias("tapa")]
    [Summary("Da um tapa.")]
    public async Task SlapAsync([Remainder] string? targetInput = null)
    {
        await SendActionAsync("slap", "✋ Tapa", targetInput);
    }

    [Command("wink")]
    [Alias("piscadinha")]
    [Summary("Envia uma piscadinha.")]
    public async Task WinkAsync([Remainder] string? targetInput = null)
    {
        await SendActionAsync("wink", "😉 Piscadinha", targetInput);
    }

    [Command("wave")]
    [Alias("tchau")]
    [Summary("Da tchau.")]
    public async Task WaveAsync([Remainder] string? targetInput = null)
    {
        await SendActionAsync("wave", "👋 Tchau", targetInput);
    }

    [Command("waifu")]
    [Summary("Mostra uma waifu (SFW/NSFW conforme config).")]
    public async Task WaifuAsync()
    {
        await SendImageOnlyAsync("waifu", "🃏 Waifu");
    }

    [Command("neko")]
    [Summary("Mostra uma neko (SFW/NSFW conforme config).")]
    public async Task NekoAsync()
    {
        await SendImageOnlyAsync("neko", "🐾 Neko");
    }

    [Command("lewd")]
    [Alias("nsfw")]
    [Summary("Mostra conteudo +18 (somente com NSFW ativado).")]
    public async Task LewdAsync()
    {
        await SendNsfwOnlyAsync();
    }

    [Command("ship")]
    [Alias("shipar")]
    [Summary("Mede a compatibilidade entre duas pessoas.")]
    public async Task ShipAsync([Remainder] string? input = null)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so pode ser usado em servidores.");
            return;
        }

        var (u1, u2) = await ResolveShipTargetsAsync(input);
        if (u1 == null || u2 == null)
        {
            await ReplyAsync("Use: ship @user1 @user2 (ou ship @user)");
            return;
        }

        if (u1.Id == u2.Id)
        {
            await ReplyAsync("Voce nao pode shipar a mesma pessoa.");
            return;
        }

        var id1 = Math.Min(u1.Id, u2.Id);
        var id2 = Math.Max(u1.Id, u2.Id);
        var stored = await _shipStore.GetAsync(id1, id2);
        var isManual = stored?.IsManual == true;
        var result = await _ship.CalculateAsync(Context.Guild, id1, id2);
        var percent = isManual ? stored!.Compatibility : result.Percent;

        var coupleName = BuildCoupleName(u1.Username, u2.Username);
        var bar = BuildBar(percent);

        var embed = EmbedHelper.CreateInfo("💘 Ship", isManual ? "Compatibilidade definida manualmente" : result.Summary)
            .AddField("Casal", $"{u1.Mention} + {u2.Mention}", false)
            .AddField("Ship", $"{coupleName}", true)
            .AddField("Compatibilidade", $"{percent}% {bar}", false);

        if (!isManual)
        {
            embed.AddField("Canais em comum", result.SharedChannelsText, false)
                .AddField("Horarios ativos", result.ActiveHoursText, false)
                .AddField("Confianca", result.ConfidenceText, true)
                .AddField("Clima", result.Title, true);
        }

        await ReplyMajesticAsync(embed);
    }

    private async Task SendActionAsync(string category, string title, string? targetInput, bool allowNsfw = false)
    {
        await TrackUserAsync();
        await Context.Channel.TriggerTypingAsync();

        var useNsfw = allowNsfw && await IsNsfwAllowedAsync();
        var imageUrl = await _waifu.GetImageUrlAsync(category, nsfw: useNsfw);
        if (string.IsNullOrWhiteSpace(imageUrl) && useNsfw)
        {
            imageUrl = await _waifu.GetImageUrlAsync(category, nsfw: false);
        }
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            await ReplyAsync("Nao consegui buscar uma imagem agora.");
            return;
        }

        var target = await ResolveTargetAsync(targetInput);
        var targetText = target.Text;
        var description = string.IsNullOrWhiteSpace(targetText)
            ? $"{Context.User.Mention} {title.ToLowerInvariant()}."
            : $"{Context.User.Mention} {title.ToLowerInvariant()} em **{targetText}**.";

        var embed = EmbedHelper.CreateMajesticWithImage(title, description, imageUrl);
        if (category == "kiss" && target.UserId.HasValue && await AreMarriedAsync(Context.User.Id, target.UserId.Value))
        {
            embed.AddField("💍 Destino selado", "Esse beijo veio de um casal casado. Bonus de fofura ativado.", false);
        }

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            if (target.UserId.HasValue && target.UserId.Value != Context.User.Id)
            {
                c.WithActionRow(new[]
                {
                    new ButtonBuilder()
                        .WithLabel("Retribuir")
                        .WithStyle(ButtonStyle.Primary)
                        .WithCustomId($"act:ret:{category}:{target.UserId.Value}:{Context.User.Id}")
                });
            }
        });

        await ReplyAsync(components: components);
    }

    private async Task SendImageOnlyAsync(string category, string title)
    {
        await TrackUserAsync();
        await Context.Channel.TriggerTypingAsync();

        var nsfwAllowed = await IsNsfwAllowedAsync();
        var nsfw = nsfwAllowed && NsfwOnlyCategories.Contains(category);
        var imageUrl = await _waifu.GetImageUrlAsync(category, nsfw);
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            await ReplyAsync("Nao consegui buscar uma imagem agora.");
            return;
        }

        var description = nsfw ? "Modo NSFW habilitado." : "Modo SFW.";
        var embed = EmbedHelper.CreateMajesticWithImage(title, description, imageUrl);
        await ReplyMajesticAsync(embed);
    }

    private async Task SendNsfwOnlyAsync()
    {
        await TrackUserAsync();

        if (!await IsNsfwAllowedAsync())
        {
            await ReplyAsync("NSFW esta desativado.");
            return;
        }

        await Context.Channel.TriggerTypingAsync();

        var category = NsfwOnlyCategories[Random.Shared.Next(NsfwOnlyCategories.Length)];
        var imageUrl = await _waifu.GetImageUrlAsync(category, nsfw: true);
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            await ReplyAsync("Nao consegui buscar uma imagem agora.");
            return;
        }

        var embed = EmbedHelper.CreateMajesticWithImage("🔥 NSFW", "Conteudo +18 liberado.", imageUrl);
        await ReplyMajesticAsync(embed);
    }

    private async Task<(ulong? UserId, string? Text)> ResolveTargetAsync(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (null, null);
        }

        var normalizedInput = input.Trim();
        var lookupInput = NormalizeLookupInput(normalizedInput);

        if (TryParseUserId(normalizedInput, out var id) || TryParseUserId(lookupInput, out id))
        {
            var user = await GetGuildUserAsync(id);
            if (user != null)
            {
                return (user.Id, user.Mention);
            }
        }

        var byName = FindMemberByName(lookupInput);
        if (byName != null)
        {
            return (byName.Id, byName.Mention);
        }

        return (null, normalizedInput.Length > 50 ? normalizedInput[..50] + "..." : normalizedInput);
    }

    private async Task<bool> IsNsfwAllowedAsync()
    {
        if (Context.Guild == null)
        {
            return false;
        }

        var config = await _configStore.GetAsync(Context.Guild.Id);
        if (!config.NsfwEnabled)
        {
            return false;
        }

        if (config.RequireNsfwChannel &&
            Context.Channel is ITextChannel textChannel && !textChannel.IsNsfw)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> AreMarriedAsync(ulong userIdA, ulong userIdB)
    {
        if (Context.Guild == null)
        {
            return false;
        }

        var id1 = Math.Min(userIdA, userIdB);
        var id2 = Math.Max(userIdA, userIdB);
        var records = await _marriageStore.GetAsync(Context.Guild.Id);
        return records.Any(r => r.UserId1 == id1 && r.UserId2 == id2);
    }

    private async Task<(IGuildUser? user1, IGuildUser? user2)> ResolveShipTargetsAsync(string? input)
    {
        if (Context.Guild == null)
        {
            return (null, null);
        }

        var candidates = new List<IGuildUser>();
        var message = Context.Message as SocketUserMessage;
        var raw = message?.Content ?? input ?? string.Empty;

        // 1) Mentions in content order
        foreach (Match match in Regex.Matches(raw, "<@!?(\\d+)>"))
        {
            if (ulong.TryParse(match.Groups[1].Value, out var id))
            {
                var gu = await GetGuildUserAsync(id);
                if (gu != null)
                {
                    AddDistinct(candidates, gu);
                }
            }
        }

        // 2) Real mentions fallback
        if (message != null && candidates.Count < 2)
        {
            foreach (var u in message.MentionedUsers)
            {
                if (u is SocketGuildUser gu && gu.Guild.Id == Context.Guild.Id)
                {
                    AddDistinct(candidates, gu);
                }
            }
        }

        // 3) IDs in text (raw)
        foreach (Match match in Regex.Matches(raw, "\\b\\d{17,20}\\b"))
        {
            if (ulong.TryParse(match.Value, out var id))
            {
                var gu = await GetGuildUserAsync(id);
                if (gu != null)
                {
                    AddDistinct(candidates, gu);
                }
            }
        }

        // 4) @name fallback
        if (candidates.Count < 2 && !string.IsNullOrWhiteSpace(raw))
        {
            var name = raw.Trim().TrimStart('@');
            var found = FindMemberByName(name);
            if (found != null)
            {
                AddDistinct(candidates, found);
            }
        }

        // 5) Reply fallback
        if (candidates.Count < 2 && message?.Reference?.MessageId.IsSpecified == true)
        {
            var refId = message.Reference.MessageId.Value;
            if (await Context.Channel.GetMessageAsync(refId) is IUserMessage replied)
            {
                if (replied.Author is SocketGuildUser ru && ru.Guild.Id == Context.Guild.Id)
                {
                    AddDistinct(candidates, ru);
                }
                else
                {
                    var ruFetched = await GetGuildUserAsync(replied.Author.Id);
                    if (ruFetched != null)
                    {
                        AddDistinct(candidates, ruFetched);
                    }
                }
            }
        }

        if (candidates.Count >= 2)
        {
            return (candidates[0], candidates[1]);
        }

        if (candidates.Count == 1)
        {
            return (Context.Guild.GetUser(Context.User.Id), candidates[0]);
        }

        return (null, null);
    }

    private SocketGuildUser? FindMemberByName(string name)
    {
        if (Context.Guild == null || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalized = name.Trim().ToLowerInvariant();
        var members = Context.Guild.Users;

        var exact = members.FirstOrDefault(u =>
            string.Equals(u.DisplayName, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.Username, name, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        var starts = members.FirstOrDefault(u =>
            u.DisplayName.StartsWith(name, StringComparison.OrdinalIgnoreCase) ||
            u.Username.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        if (starts != null) return starts;

        return members.FirstOrDefault(u =>
            u.DisplayName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
            u.Username.Contains(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddDistinct(List<IGuildUser> list, IGuildUser user)
    {
        if (list.All(u => u.Id != user.Id))
        {
            list.Add(user);
        }
    }

    private static string BuildCoupleName(string name1, string name2)
    {
        var left = name1.Length <= 3 ? name1 : name1[..(name1.Length / 2)];
        var right = name2.Length <= 3 ? name2 : name2[(name2.Length / 2)..];
        return left + right;
    }

    private static string BuildBar(int percent)
    {
        var total = 10;
        var filled = (int)Math.Round(percent / 10.0);
        return "[" + new string('█', filled) + new string('░', total - filled) + "]";
    }

    private async Task<IGuildUser?> GetGuildUserAsync(ulong userId)
    {
        if (Context.Guild == null)
        {
            return null;
        }

        var cached = Context.Guild.GetUser(userId);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            return await Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, userId);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeLookupInput(string input)
    {
        return input.Trim().TrimStart('@').Trim();
    }

    private static bool TryParseUserId(string input, out ulong userId)
    {
        userId = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (MentionUtils.TryParseUser(input, out userId))
        {
            return true;
        }

        return ulong.TryParse(input.Trim(), out userId);
    }
}
