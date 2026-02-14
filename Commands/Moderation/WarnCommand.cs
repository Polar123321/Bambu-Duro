using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.Moderation;

public sealed class WarnCommand : CommandBase
{
    private readonly IModerationActionStore _store;

    public WarnCommand(
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

    [Command("warn")]
    [Alias("avisar")]
    [Summary("Avisa um usuario com confirmação. Exige motivo.")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task WarnAsync(string target, [Remainder] string reason)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            await ReplyAsync($"Informe o usuario. Ex: {Config.Value.Prefix}warn @user motivo");
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            await ReplyAsync($"Motivo obrigatorio. Ex: {Config.Value.Prefix}warn @user motivo");
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
            await ReplyAsync("Voce nao pode aplicar warn em si mesmo.");
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
            await ReplyAsync("Voce nao pode aplicar warn no dono do servidor.");
            return;
        }

        if (moderator.Id != Context.Guild.OwnerId && moderator.Hierarchy <= targetGuildUser.Hierarchy)
        {
            await ReplyAsync("Voce so pode aplicar warn em usuarios abaixo do seu cargo.");
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        _store.Add(new ModerationAction(
            token,
            ModerationActionType.Warn,
            Context.Guild.Id,
            user.Id,
            Context.User.Id,
            Context.Channel.Id,
            0,
            0,
            reason.Trim(),
            DateTime.UtcNow),
            TimeSpan.FromMinutes(5));

        var embed = EmbedHelper.CreateWarning("⚠️ Confirmar aviso",
                $"Alvo: **{user.Username}** ({user.Id})\nMotivo: **{reason.Trim()}**")
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
