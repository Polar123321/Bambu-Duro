using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.Moderation;

public sealed class MuteCommand : CommandBase
{
    private readonly IModerationActionStore _store;

    public MuteCommand(
        IModerationActionStore store,
        EmbedHelper embedHelper,
        Microsoft.Extensions.Options.IOptions<ConsoleApp4.Configuration.BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _store = store;
    }

    [Command("mute")]
    [Summary("Silencia um usuario com confirmação. Exige motivo.")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task MuteAsync(string target, string duration, [Remainder] string reason)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            await ReplyAsync($"Informe o usuario. Ex: {Config.Value.Prefix}mute @user 10m motivo");
            return;
        }

        if (string.IsNullOrWhiteSpace(duration))
        {
            await ReplyAsync($"Informe a duracao. Ex: {Config.Value.Prefix}mute @user 10m motivo");
            return;
        }

        if (!TryParseDurationMinutes(duration, out var minutes) || minutes <= 0)
        {
            await ReplyAsync($"Duracao invalida. Use minutos (ex: `10`) ou sufixos (ex: `10m`, `1h`, `2d`). Ex: {Config.Value.Prefix}mute @user 1h motivo");
            return;
        }

        // Discord timeout has a hard cap (28 days). Fail fast so we don't create an action that can never succeed.
        const int MaxTimeoutMinutes = 28 * 24 * 60;
        if (minutes > MaxTimeoutMinutes)
        {
            await ReplyAsync($"Duracao maxima do mute (timeout) e 28d. Ex: {Config.Value.Prefix}mute @user 7d motivo");
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            await ReplyAsync($"Motivo obrigatorio. Ex: {Config.Value.Prefix}mute @user 10m spam");
            return;
        }

        var user = await ResolveTargetAsync(Context.Guild, target);
        if (user == null)
        {
            await ReplyAsync("Nao encontrei o usuario informado neste servidor.");
            return;
        }

        if (Context.User.Id == user.Id)
        {
            await ReplyAsync("Voce nao pode aplicar mute em si mesmo.");
            return;
        }

        if (user is not IGuildUser targetGuildUser)
        {
            await ReplyAsync("Nao consegui validar o usuario no servidor.");
            return;
        }

        if (Context.User is not SocketGuildUser moderator)
        {
            await ReplyAsync("Nao consegui validar suas permissoes.");
            return;
        }

        if (targetGuildUser.Id == Context.Guild.OwnerId)
        {
            await ReplyAsync("Voce nao pode aplicar mute no dono do servidor.");
            return;
        }

        if (moderator.Id != Context.Guild.OwnerId && moderator.Hierarchy <= targetGuildUser.Hierarchy)
        {
            await ReplyAsync("Voce so pode aplicar mute em usuarios abaixo do seu cargo.");
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        _store.Add(new ModerationAction(
            token,
            ModerationActionType.Mute,
            Context.Guild.Id,
            user.Id,
            Context.User.Id,
            Context.Channel.Id,
            0,
            minutes,
            reason.Trim(),
            DateTime.UtcNow),
            TimeSpan.FromMinutes(5));

        var embed = EmbedHelper.CreateWarning("🔇 Confirmar mute",
                $"Alvo: **{user.Username}** ({user.Id})\nTempo: **{minutes} min**\nMotivo: **{reason.Trim()}**")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Confirmar")
                    .WithCustomId($"mod:confirm:{token}")
                    .WithStyle(ButtonStyle.Danger),
                new ButtonBuilder()
                    .WithLabel("Cancelar")
                    .WithCustomId($"mod:cancel:{token}")
                    .WithStyle(ButtonStyle.Secondary)
            });
        });

        await ReplyAsync(components: components);
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
                // Avoid relying on gateway member cache; fetch from REST when needed.
                var rest = await Context.Client.Rest.GetGuildUserAsync(guild.Id, userId);
                return rest;
            }
            catch
            {
                return null;
            }
        }

        // Best-effort fallback for plain text inputs (only works for cached members).
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

    private static bool TryParseDurationMinutes(string input, out int minutes)
    {
        minutes = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var s = input.Trim().ToLowerInvariant();

        // Plain number = minutes
        if (int.TryParse(s, out var m))
        {
            minutes = m;
            return true;
        }

        // Support compact forms like: 10m, 1h, 2d, 1h30m, 2d4h, etc.
        // Units: m=minutes, h=hours, d=days
        var total = 0L;
        var i = 0;
        while (i < s.Length)
        {
            if (!char.IsDigit(s[i]))
            {
                return false;
            }

            var start = i;
            while (i < s.Length && char.IsDigit(s[i]))
            {
                i++;
            }

            if (!long.TryParse(s[start..i], out var value) || value < 0)
            {
                return false;
            }

            if (i >= s.Length)
            {
                return false; // missing unit
            }

            var unit = s[i];
            i++;

            checked
            {
                total += unit switch
                {
                    'm' => value,
                    'h' => value * 60,
                    'd' => value * 60 * 24,
                    _ => throw new InvalidOperationException("invalid unit")
                };
            }
        }

        if (total > int.MaxValue)
        {
            return false;
        }

        minutes = (int)total;
        return true;
    }
}
