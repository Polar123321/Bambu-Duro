using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class WelcomeChannelCommand : CommandBase
{
    private readonly IGuildConfigStore _configStore;

    public WelcomeChannelCommand(
        IGuildConfigStore configStore,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _configStore = configStore;
    }

    [Command("setwelcomechannel")]
    [Alias("welcomechannel")]
    [Summary("Define o canal de boas-vindas do servidor.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task SetWelcomeChannelAsync(ITextChannel? channel = null)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so pode ser usado em servidores.");
            return;
        }

        var targetChannel = channel ?? Context.Channel as ITextChannel;
        if (targetChannel == null)
        {
            await ReplyAsync("Nao consegui identificar o canal.");
            return;
        }

        var config = await _configStore.GetAsync(Context.Guild.Id);
        config.WelcomeChannelId = targetChannel.Id;
        await _configStore.SaveAsync(Context.Guild.Id, config);

        var embed = EmbedHelper.CreateSuccess("Canal de boas-vindas definido!",
            $"Mensagens de boas-vindas serao enviadas em {targetChannel.Mention}.");

        await ReplyMajesticAsync(embed);
    }
}
