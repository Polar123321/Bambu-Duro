using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class RevokeWarnCommand : CommandBase
{
    private readonly IWarnService _warns;

    public RevokeWarnCommand(
        IWarnService warns,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _warns = warns;
    }

    [Command("revokewarn")]
    [Alias("unwarn", "removewarn")]
    [Summary("Revoga warns de um usuario. Ex: *revokewarn @user last|all|<n>")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task RevokeWarnAsync(string target, string? which = null)
    {
        await TrackUserAsync();

        if (Context.Guild is not SocketGuild guild)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var user = await ResolveTargetAsync(guild, target);
        if (user == null)
        {
            await ReplyAsync("Nao encontrei o usuario informado neste servidor.");
            return;
        }

        which = (which ?? "last").Trim().ToLowerInvariant();

        if (which == "all" || which == "todos")
        {
            var count = await _warns.RevokeAllAsync(guild.Id, user.Id, Context.User.Id);
            await AllWarnListLivePanel.TryRefreshAsync(Context.Client, EmbedHelper, _warns, guild.Id);
            await ReplyMajesticAsync("Warns revogados", $"{user.Mention}: {count} warn(s) revogado(s).");
            return;
        }

        var warns = await _warns.GetActiveWarnsAsync(guild.Id, user.Id);
        if (warns.Count == 0)
        {
            await ReplyMajesticAsync("Warns", $"{user.Mention} nao possui warns ativos.");
            return;
        }

        WarnEntryPicker picker = WarnEntryPicker.From(which);
        var selected = picker.Pick(warns);
        if (selected == null)
        {
            await ReplyAsync("Formato invalido. Use `last`, `all` ou um numero (ex: `1`).");
            return;
        }

        var ok = await _warns.RevokeAsync(guild.Id, selected.Id, Context.User.Id);
        if (!ok)
        {
            await ReplyAsync("Nao consegui revogar esse warn (talvez ja tenha sido revogado).");
            return;
        }

        await AllWarnListLivePanel.TryRefreshAsync(Context.Client, EmbedHelper, _warns, guild.Id);

        await ReplyMajesticAsync("Warn revogado",
            $"{user.Mention}\nID: `{selected.Id}`\nMotivo: {selected.Reason}");
    }

    private async Task<IUser?> ResolveTargetAsync(SocketGuild guild, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (TryParseUserId(input, out var userId))
        {
            var cached = guild.GetUser(userId);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                return await Context.Client.Rest.GetGuildUserAsync(guild.Id, userId);
            }
            catch
            {
                return null;
            }
        }

        var normalized = input.Trim();
        var byNick = guild.Users.FirstOrDefault(u =>
            string.Equals(u.Nickname, normalized, StringComparison.OrdinalIgnoreCase));
        if (byNick != null)
        {
            return byNick;
        }

        return guild.Users.FirstOrDefault(u =>
            string.Equals(u.Username, normalized, StringComparison.OrdinalIgnoreCase));
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

    private sealed class WarnEntryPicker
    {
        private readonly int? _index; 

        private WarnEntryPicker(int? index)
        {
            _index = index;
        }

        public static WarnEntryPicker From(string which)
        {
            if (which == "last" || which == "ultimo")
            {
                return new WarnEntryPicker(index: 1);
            }

            if (int.TryParse(which, out var n) && n > 0)
            {
                return new WarnEntryPicker(index: n);
            }

            return new WarnEntryPicker(index: null);
        }

        public WarnEntry? Pick(IReadOnlyList<WarnEntry> warns)
        {
            if (_index == null)
            {
                return null;
            }

            var idx = _index.Value - 1;
            if (idx < 0 || idx >= warns.Count)
            {
                return null;
            }

            
            return warns[idx];
        }
    }
}
