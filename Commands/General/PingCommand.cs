using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.General;

public sealed class PingCommand : CommandBase
{
    public PingCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
    }

    [Command("ping")]
    [Summary("Verifica a latência do bot.")]
    [Attributes.Cooldown(3)]
    public async Task PingAsync()
    {
        await TrackUserAsync();

        var embed = EmbedHelper
            .CreateInfo("🏓 Pong!", $"Latência: **{Context.Client.Latency}ms**");

        await ReplyMajesticAsync(embed);
    }
}
