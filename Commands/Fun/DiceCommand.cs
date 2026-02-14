using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Fun;

public sealed class DiceCommand : CommandBase
{
    private static readonly Random Random = new();

    public DiceCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
    }

    [Command("dado")]
    [Alias("dice")]
    [Summary("Rola um dado de 6 lados.")]
    public async Task DiceAsync()
    {
        await TrackUserAsync();

        var result = Random.Next(1, 7);
        var embed = EmbedHelper.CreateInfo("🎲 Dado Rolado", $"Resultado: **{result}**");
        await ReplyMajesticAsync(embed);
    }
}
