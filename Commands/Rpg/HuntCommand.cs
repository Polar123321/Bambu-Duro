using Discord.Commands;
using ConsoleApp4.Attributes;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Rpg;

public sealed class HuntCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public HuntCommand(
        IEconomyService economy,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _economy = economy;
    }

    [Command("cacar")]
    [Alias("hunt")]
    [Summary("Vai a uma cacada para ganhar EXP e itens.")]
    [Cooldown(1200)]
    public async Task HuntAsync()
    {
        await TrackUserAsync();
        var result = await _economy.HuntAsync(Context.User.Id, Context.User.Username);

        var embed = EmbedHelper.CreateSuccess("🏹 Cacada", result.Message)
            .AddField("Nivel", result.NewLevel, true)
            .AddField("EXP", result.NewExp, true);

        await ReplyMajesticAsync(embed);
    }
}
