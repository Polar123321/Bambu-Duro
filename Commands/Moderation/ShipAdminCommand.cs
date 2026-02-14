using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using System.Text.RegularExpressions;

namespace ConsoleApp4.Commands.Moderation;

public sealed class ShipAdminCommand : CommandBase
{
    private readonly IShipStore _shipStore;

    public ShipAdminCommand(
        IShipStore shipStore,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _shipStore = shipStore;
    }

    [Command("setship")]
    [Summary("Define manualmente a compatibilidade de um casal (somente dono do bot).")]
    public async Task SetShipAsync([Remainder] string? input = null)
    {
        await TrackUserAsync();

        if (Context.User.Id != Config.Value.OwnerUserId)
        {
            await ReplyAsync("Apenas o dono do bot pode usar este comando.");
            return;
        }

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so pode ser usado em servidores.");
            return;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            await ReplyAsync("Use: setship @user1 @user2 0-100");
            return;
        }

        var (user1, user2) = await ResolveTwoUsersAsync(input);
        if (user1 == null || user2 == null)
        {
            await ReplyAsync("Nao consegui identificar os dois usuarios. Use: setship @user1 @user2 0-100");
            return;
        }

        var percent = ExtractPercent(input);
        if (percent == null)
        {
            await ReplyAsync("Informe a porcentagem entre 0 e 100. Ex: setship @user1 @user2 75");
            return;
        }

        var id1 = Math.Min(user1.Id, user2.Id);
        var id2 = Math.Max(user1.Id, user2.Id);
        await _shipStore.SaveAsync(new ShipRecord(id1, id2, percent.Value, DateTime.UtcNow, IsManual: true));

        var embed = EmbedHelper.CreateSuccess("Compatibilidade definida",
            $"{user1.Mention} + {user2.Mention} agora tem **{percent.Value}%**.");

        await ReplyMajesticAsync(embed);
    }

    private int? ExtractPercent(string input)
    {
        foreach (Match match in Regex.Matches(input, "\\b\\d{1,3}\\b"))
        {
            if (int.TryParse(match.Value, out var value) && value >= 0 && value <= 100)
            {
                return value;
            }
        }

        return null;
    }

    private async Task<(IGuildUser? user1, IGuildUser? user2)> ResolveTwoUsersAsync(string input)
    {
        if (Context.Guild == null)
        {
            return (null, null);
        }

        var ids = new List<ulong>();
        foreach (Match match in Regex.Matches(input, "<@!?(\\d+)>"))
        {
            if (ulong.TryParse(match.Groups[1].Value, out var id))
            {
                ids.Add(id);
            }
        }

        if (ids.Count < 2)
        {
            foreach (Match match in Regex.Matches(input, "\\b\\d{17,20}\\b"))
            {
                if (ulong.TryParse(match.Value, out var id))
                {
                    ids.Add(id);
                }
            }
        }

        var u1 = ids.Count > 0 ? await GetGuildUserAsync(ids[0]) : null;
        var u2 = ids.Count > 1 ? await GetGuildUserAsync(ids[1]) : null;
        return (u1, u2);
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
}
