using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Fun;

public sealed class CoinCommand : CommandBase
{
    private static readonly Random Random = new();

    public CoinCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
    }

    [Command("moeda")]
    [Alias("coin")]
    [Summary("Joga uma moeda (cara ou coroa).")]
    public async Task CoinAsync()
    {
        await TrackUserAsync();

        var result = Random.Next(0, 2) == 0 ? "Cara" : "Coroa";
        var embed = EmbedHelper.CreateInfo("🪙 Moeda Lançada", $"Resultado: **{result}**");
        await ReplyMajesticAsync(embed);
    }
}
