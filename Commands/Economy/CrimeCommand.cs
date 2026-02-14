using Discord.Commands;
using ConsoleApp4.Attributes;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Economy;

public sealed class CrimeCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public CrimeCommand(
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

    [Command("crime")]
    [Alias("roubar", "assalto")]
    [Summary("Tenta cometer um crime para ganhar moedas (ou perder!).")]
    [Cooldown(2700)]
    public async Task CrimeAsync()
    {
        await TrackUserAsync();
        var result = await _economy.CrimeAsync(Context.User.Id, Context.User.Username);

        var title = result.Success ? "🕵️ Crime bem sucedido" : "🚓 Crime falhou";
        var embed = result.Success
            ? EmbedHelper.CreateSuccess(title, result.Message)
            : EmbedHelper.CreateWarning(title, result.Message);

        embed.AddField("Saldo", result.NewBalance, true)
             .AddField("Nivel", result.NewLevel, true);

        await ReplyMajesticAsync(embed);
    }
}
