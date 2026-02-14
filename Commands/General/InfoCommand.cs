using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.General;

public sealed class InfoCommand : CommandBase
{
    public InfoCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
    }

    [Command("info")]
    [Summary("Mostra informações do bot.")]
    public async Task InfoAsync()
    {
        await TrackUserAsync();

        var socketClient = Context.Client;
        var embed = EmbedHelper.CreateInfo("🤖 Informações do Bot", "Resumo rápido do bot")
            .AddField("Servidores", socketClient.Guilds.Count, true)
            .AddField("Usuários", socketClient.Guilds.Sum(g => g.MemberCount), true)
            .AddField("Latência", $"{socketClient.Latency}ms", true)
            .AddField("Framework", ".NET 6+", true)
            .AddField("Biblioteca", "Discord.Net 3.x", true)
            .WithThumbnailUrl(socketClient.CurrentUser.GetAvatarUrl());

        await ReplyMajesticAsync(embed);
    }
}
