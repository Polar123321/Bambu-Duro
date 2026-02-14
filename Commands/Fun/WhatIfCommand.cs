using Discord;
using Discord.Commands;
using ConsoleApp4.Attributes;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Fun;

public sealed class WhatIfCommand : CommandBase
{
    private readonly IGroqChatService _groq;

    public WhatIfCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService,
        IGroqChatService groq)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _groq = groq;
    }

    [Command("whatif")]
    [Alias("ese", "e-se", "eSe")]
    [Summary("Analisa um cenario 'e se...' usando IA e deduz consequencias provaveis.")]
    [Cooldown(15)]
    public async Task WhatIfAsync([Remainder] string scenario)
    {
        await TrackUserAsync();

        
        await Context.Channel.TriggerTypingAsync();

        var answer = await _groq.WhatIfAsync(scenario, Context.User.Username);

        
        if (answer.Length > 3800)
        {
            answer = answer[..3800] + "\n\n(Resposta cortada)";
        }

        var embed = EmbedHelper.CreateMajestic("What if (E se)...", answer)
            .WithColor(Discord.Color.Blue);

        await ReplyAsync(components: EmbedHelper.BuildCv2(embed));
    }
}
