using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.General;

public sealed class TestMessageCommand : CommandBase
{
    private const int MaxMessageLength = 1000;
    private readonly IModerationActionStore _store;

    public TestMessageCommand(
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

    [Command("testmsg")]
    [Alias("testmessage", "teste")]
    [Summary("Envia uma mensagem de teste com confirmação.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task TestMessageAsync([Remainder] string message)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            await ReplyAsync($"Uso: {Config.Value.Prefix}testmsg <mensagem>");
            return;
        }

        var trimmed = message.Trim();
        if (trimmed.Length > MaxMessageLength)
        {
            await ReplyAsync($"Mensagem muito longa. Maximo: {MaxMessageLength} caracteres.");
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        _store.Add(new ModerationAction(
            token,
            ModerationActionType.TestMessage,
            Context.Guild.Id,
            0,
            Context.User.Id,
            Context.Channel.Id,
            1,
            0,
            trimmed,
            DateTime.UtcNow),
            TimeSpan.FromMinutes(5));

        var embed = EmbedHelper.CreateWarning("✅ Confirmar mensagem de teste",
                $"Canal: <#{Context.Channel.Id}>\nMensagem:\n{trimmed}")
            .WithFooter("Confirme ou cancele abaixo");

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Confirmar")
                    .WithCustomId($"mod:confirm:{token}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Cancelar")
                    .WithCustomId($"mod:cancel:{token}")
                    .WithStyle(ButtonStyle.Secondary)
            });
        });

        await ReplyAsync(components: components);
    }
}
