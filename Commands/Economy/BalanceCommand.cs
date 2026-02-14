using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Economy;

public sealed class BalanceCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public BalanceCommand(
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

    [Command("saldo")]
    [Alias("balance", "bal", "money")]
    [Summary("Mostra seu saldo de moedas.")]
    public async Task BalanceAsync()
    {
        await TrackUserAsync();

        var balance = await _economy.GetBalanceAsync(Context.User.Id, Context.User.Username);
        var embed = EmbedHelper.CreateInfo("💰 Seu saldo", "Resumo rápido da sua economia")
            .AddField("Moedas", $"{balance}", true)
            .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());

        await ReplyMajesticAsync(embed);
    }
}
