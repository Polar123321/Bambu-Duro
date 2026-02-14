using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Configuration;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ConsoleApp4.Commands.Moderation;

public sealed class AllWarnListCommand : CommandBase
{
    private readonly IWarnService _warns;

    public AllWarnListCommand(
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

    [Command("allwarnlist")]
    [Alias("allwarns", "awarnlist")]
    [Summary("Lista todos os warns ativos do servidor. Ex: *allwarnlist [pagina]")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task AllWarnListAsync(int page = 1)
    {
        await TrackUserAsync();

        if (Context.Guild is not SocketGuild guild)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var warns = await _warns.GetAllActiveWarnsAsync(guild.Id);
        var components = AllWarnListLivePanel.BuildPublicComponents(EmbedHelper, warns, page);
        var message = await ReplyAsync(components: components);
        AllWarnListLivePanel.Register(guild.Id, Context.Channel.Id, message.Id);
    }
}
