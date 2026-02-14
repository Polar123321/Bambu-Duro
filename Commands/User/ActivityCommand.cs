using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.User;

public sealed class ActivityCommand : CommandBase
{
    private readonly IUserGuildStatsService _stats;

    public ActivityCommand(
        IUserGuildStatsService stats,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _stats = stats;
    }

    [Command("atividade")]
    [Alias("activity", "mensagens", "convites", "userstats")]
    [Summary("Mostra quantas mensagens e convites você fez no servidor.")]
    public async Task ActivityAsync([Remainder] string? target = null)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando só funciona em servidores.");
            return;
        }

        var resolved = await ResolveTargetAsync(Context.Guild, target);
        if (resolved == null)
        {
            await ReplyAsync("Nao encontrei o usuario informado neste servidor.");
            return;
        }

        var userId = resolved.Id;
        var displayName = resolved.Username;
        var mention = resolved.Mention;
        var avatarUrl = resolved.GetAvatarUrl(size: 256) ?? resolved.GetDefaultAvatarUrl();

        var (messages, invites) = await _stats.GetCountsAsync(Context.Guild.Id, userId);

        var embed = EmbedHelper.CreateInfo($"📊 Atividade de {displayName}", $"{mention}")
            .AddField("Mensagens (servidor)", messages.ToString("N0"), true)
            .AddField("Convites (servidor)", invites.ToString("N0"), true)
            .WithThumbnailUrl(avatarUrl);

        await ReplyMajesticAsync(embed);
    }

    private async Task<IUser?> ResolveTargetAsync(SocketGuild guild, string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Context.User;
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
                var rest = await Context.Client.Rest.GetGuildUserAsync(guild.Id, userId);
                return rest;
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

        var byName = guild.Users.FirstOrDefault(u =>
            string.Equals(u.Username, normalized, StringComparison.OrdinalIgnoreCase));
        return byName;
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

        var trimmed = input.Trim();
        return ulong.TryParse(trimmed, out userId);
    }
}
