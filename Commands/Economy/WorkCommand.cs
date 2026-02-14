using Discord.Commands;
using ConsoleApp4.Attributes;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Economy;

public sealed class WorkCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public WorkCommand(
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

    [Command("trabalhar")]
    [Alias("work")]
    [Summary("Trabalha para ganhar moedas e EXP.")]
    [Cooldown(1800)]
    public async Task WorkAsync()
    {
        await TrackUserAsync();
        var result = await _economy.WorkAsync(Context.User.Id, Context.User.Username);

        var embed = EmbedHelper.CreateSuccess("🛠️ Trabalho concluido", result.Message)
            .AddField("Saldo", result.NewBalance, true)
            .AddField("Nivel", result.NewLevel, true);

        await ReplyMajesticAsync(embed);
    }
}
