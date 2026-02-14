using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Rpg;

public sealed class StatsCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public StatsCommand(
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

    [Command("stats")]
    [Alias("status", "rpg")]
    [Summary("Mostra suas estatisticas de RPG.")]
    public async Task StatsAsync()
    {
        await TrackUserAsync();
        var user = await UserService.GetOrCreateAsync(Context.User.Id, Context.User.Username);

        var embed = EmbedHelper.CreateInfo($"⚔️ Status de {Context.User.Username}", "Resumo do seu progresso")
            .AddField("Nivel", user.Level, true)
            .AddField("EXP", user.Experience, true)
            .AddField("Moedas", user.Coins, true)
            .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());

        await ReplyMajesticAsync(embed);
    }
}
