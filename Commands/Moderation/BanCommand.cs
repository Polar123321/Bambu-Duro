using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.Moderation;

public sealed class BanCommand : CommandBase
{
    private readonly IModerationActionStore _store;

    public BanCommand(
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

    [Command("ban")]
    [Summary("Bane um usuario com confirmação. Exige motivo.")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task BanAsync(SocketGuildUser user, [Remainder] string reason)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (user == null)
        {
            await ReplyAsync("Usuario invalido.");
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            await ReplyAsync("Motivo obrigatorio. Ex: !ban @user spam");
            return;
        }

        if (Context.User.Id == user.Id)
        {
            await ReplyAsync("Voce nao pode banir a si mesmo.");
            return;
        }

        if (user.Id == Context.Guild.OwnerId)
        {
            await ReplyAsync("Voce nao pode banir o dono do servidor.");
            return;
        }

        if (Context.User is not SocketGuildUser moderator)
        {
            await ReplyAsync("Nao consegui validar suas permissoes.");
            return;
        }

        if (moderator.Id != Context.Guild.OwnerId && moderator.Hierarchy <= user.Hierarchy)
        {
            await ReplyAsync("Voce so pode banir usuarios abaixo do seu cargo.");
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        _store.Add(new ModerationAction(
            token,
            ModerationActionType.Ban,
            Context.Guild.Id,
            user.Id,
            Context.User.Id,
            Context.Channel.Id,
            0,
            0,
            reason.Trim(),
            DateTime.UtcNow),
            TimeSpan.FromMinutes(5));

        var embed = EmbedHelper.CreateWarning("⛔ Confirmar ban",
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
}
