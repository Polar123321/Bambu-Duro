using Discord.Commands;
using ConsoleApp4.Attributes;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Economy;

public sealed class DailyCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public DailyCommand(
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

    [Command("daily")]
    [Alias("diario")]
    [Summary("Recebe seu bonus diario de moedas.")]
    [Cooldown(86400)]
    public async Task DailyAsync()
    {
        await TrackUserAsync();
        var result = await _economy.DailyAsync(Context.User.Id, Context.User.Username);

        var embed = EmbedHelper.CreateSuccess("🎁 Bonus diario", result.Message)
            .AddField("Saldo", result.NewBalance, true);

        await ReplyMajesticAsync(embed);
    }
}
