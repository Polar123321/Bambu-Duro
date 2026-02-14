using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.Moderation;

public sealed class ClearCommand : CommandBase
{
    private readonly IModerationActionStore _store;

    public ClearCommand(
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

    [Command("clear")]
    [Summary("Apaga mensagens com confirmação. Exige motivo.")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task ClearAsync(int amount, [Remainder] string reason)
    {
        await TrackUserAsync();

        if (string.IsNullOrWhiteSpace(reason))
        {
            await ReplyAsync("Motivo obrigatorio. Ex: !clear 10 spam");
            return;
        }

        var safeAmount = Math.Clamp(amount, 1, 100);
        var token = Guid.NewGuid().ToString("N");
        _store.Add(new ModerationAction(
            token,
            ModerationActionType.Clear,
            Context.Guild.Id,
            0,
            Context.User.Id,
            Context.Channel.Id,
            safeAmount,
            0,
            reason.Trim(),
            DateTime.UtcNow),
            TimeSpan.FromMinutes(5));

        var embed = EmbedHelper.CreateWarning("🧹 Confirmar limpeza",
                $"Voce esta prestes a apagar **{safeAmount}** mensagens.\nMotivo: **{reason.Trim()}**")
            .WithFooter("Confirme ou cancele abaixo");

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
